using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1f;
    [SerializeField] float boostModifier = 1f;
    [SerializeField] float maxBoostDuration = 2f;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] float safetyMargin = 0.05f; // Small buffer distance from walls

    Vector2 moveDirection;
    public Vector2 MoveDirection { get { return moveDirection; } private set { moveDirection = value; } }

    Vector2 lastMoveDirection;
    float boostDuration = 2f;
    bool canBoost = true;
    bool isBoost = false;
    Collider2D playerCollider;

    // For visual debugging
    private bool hasLoggedWallIssue = false;
    private BoostUI boostUI;

    void Start()
    {
        moveDirection = Vector2.up;
        lastMoveDirection = Vector2.up;
        playerCollider = GetComponent<Collider2D>();
        boostUI = FindFirstObjectByType<BoostUI>();
        if (boostUI == null)
        {
            Debug.LogWarning("BoostUI not found in the scene. Boost UI will not be updated.");
        }

        // If no wall layer is set, use default
        if (wallLayer.value == 0)
        {
            wallLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("Wall Layer not set - using Default layer");
        }
    }

    void Update()
    {
        // Emergency resolution if we're inside a wall
        bool wasOverlapping = false;
        Collider2D overlapCollider = null;
        if (IsOverlappingWall(out overlapCollider))
        {
            wasOverlapping = true;
            if (!hasLoggedWallIssue)
            {
                Debug.LogWarning($"Player inside wall: {overlapCollider.name}. Attempting to resolve.");
                hasLoggedWallIssue = true;
            }

            // Try to resolve the overlap
            ResolveWallOverlap(overlapCollider);
        }
        else if (!wasOverlapping)
        {
            hasLoggedWallIssue = false;
        }

        // Normal movement
        if (!WallAhead(moveDirection))
        {
            Vector2 nextPosition = CalculateNextPosition(moveDirection);
            if (!WillPositionIntersectWall(nextPosition))
            {
                ProcessMovement(moveDirection);
            }
            else
            {
                FindAlternativeDirection();
            }
        }
        else
        {
            FindAlternativeDirection();
        }

        ProcessBoost();
        RegenerateBoost();
    }

    bool IsOverlappingWall(out Collider2D wallCollider)
    {
        wallCollider = null;
        if (playerCollider == null) return false;

        // Get all overlapping colliders
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;

        var results = new System.Collections.Generic.List<Collider2D>();
        Physics2D.OverlapCollider(playerCollider, filter, results);

        // Return true if we're overlapping with any wall
        foreach (Collider2D collider in results)
        {
            if (collider.CompareTag("Wall"))
            {
                wallCollider = collider;
                return true;
            }
        }

        return false;
    }

    // Push player out of wall overlap
    void ResolveWallOverlap(Collider2D wallCollider)
    {
        if (playerCollider == null || wallCollider == null) return;

        // Get the direction away from the wall's center
        Vector2 playerPosition = (Vector2)transform.position;
        Vector2 closestPointOnWall = wallCollider.bounds.ClosestPoint(playerPosition);

        // Calculate direction away from the wall
        Vector2 awayFromWall = (playerPosition - closestPointOnWall).normalized;

        // If the direction is zero (player at exact center of wall), use a default direction
        if (awayFromWall.magnitude < 0.001f)
        {
            awayFromWall = Vector2.up;  // Default push direction
        }

        // Calculate how much to move
        float currentDistance = Vector2.Distance(playerPosition, closestPointOnWall);
        float playerRadius = Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y);
        float pushDistance = (playerRadius - currentDistance) + safetyMargin;

        // Only push if we need to
        if (pushDistance > 0)
        {
            transform.position = playerPosition + (awayFromWall * pushDistance);
            Debug.Log($"Resolved wall overlap: moved {pushDistance} units in direction {awayFromWall}");
        }

        // If still overlapping, try the fallback method
        if (IsOverlappingWall(out _))
        {
            FallbackResolution();
        }
    }

    void FallbackResolution()
    {
        // Try moving in each of the four cardinal directions
        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };

        float pushDistance = 0.2f; // Medium push distance

        foreach (Vector2 dir in directions)
        {
            Vector2 potentialPosition = (Vector2)transform.position + dir * pushDistance;

            if (!WillPositionIntersectWall(potentialPosition))
            {
                transform.position = potentialPosition;
                return;
            }
        }

        // If still stuck, try a larger push opposite to current direction
        transform.position = (Vector2)transform.position - moveDirection * pushDistance * 2;
    }

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            input = new Vector2(Mathf.Sign(input.x), 0);
        }
        else if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            input = new Vector2(0, Mathf.Sign(input.y));
        }

        if (input != Vector2.zero && (input + lastMoveDirection) != Vector2.zero)
        {
            Vector2 nextPosition = CalculateNextPosition(input);

            if (!WallAhead(input) && !WillPositionIntersectWall(nextPosition))
            {
                moveDirection = input;
                lastMoveDirection = moveDirection;
            }
            else
            {
                Debug.Log("Wall ahead or collision at next position! Cannot move in that direction.");
            }
        }
    }

    bool WallAhead(Vector2 direction)
    {
        // First check with a raycast from slightly behind the player
        Vector2 origin = (Vector2)transform.position - (direction * 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance + 0.1f, wallLayer);

        Debug.DrawRay(origin, direction * (wallCheckDistance + 0.1f), hit.collider != null ? Color.red : Color.green, 0.1f);

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            return true;
        }

        // Second check with a box cast for more coverage
        Vector2 boxSize = playerCollider != null ?
            playerCollider.bounds.size * 0.8f :
            new Vector2(0.8f, 0.8f);

        hit = Physics2D.BoxCast(
            transform.position,
            boxSize,
            0f,
            direction,
            wallCheckDistance,
            wallLayer);

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            return true;
        }

        return false;
    }

    Vector2 CalculateNextPosition(Vector2 direction)
    {
        Vector2 currentPosition = transform.position;
        Vector2 movement = direction * moveSpeed * Time.deltaTime;
        return currentPosition + movement;
    }

    void FindAlternativeDirection()
    {
        // Try perpendicular directions
        Vector2 rightPerp = new Vector2(-moveDirection.y, moveDirection.x);
        Vector2 leftPerp = new Vector2(moveDirection.y, -moveDirection.x);

        bool rightClear = !WallAhead(rightPerp) && !WillPositionIntersectWall(CalculateNextPosition(rightPerp));
        bool leftClear = !WallAhead(leftPerp) && !WillPositionIntersectWall(CalculateNextPosition(leftPerp));

        if (rightClear)
        {
            moveDirection = rightPerp;
            lastMoveDirection = moveDirection;
            ProcessMovement(moveDirection);
        }
        else if (leftClear)
        {
            moveDirection = leftPerp;
            lastMoveDirection = moveDirection;
            ProcessMovement(moveDirection);
        }
        else
        {
            Vector2 opposite = -moveDirection;
            if (!WallAhead(opposite) && !WillPositionIntersectWall(CalculateNextPosition(opposite)))
            {
                moveDirection = opposite;
                lastMoveDirection = moveDirection;
                ProcessMovement(moveDirection);
            }
            else
            {
                Debug.Log("Surrounded by walls, cannot move!");
            }
        }
    }

    public void ChangeDirection(Vector2 newDirection)
    {
        Vector2 nextPosition = CalculateNextPosition(newDirection);

        if (!WallAhead(newDirection) && !WillPositionIntersectWall(nextPosition))
        {
            moveDirection = newDirection;
            lastMoveDirection = newDirection;
        }
        else
        {
            Debug.Log("Cannot change direction - wall in the way");
            FindAlternativeDirection();
        }
    }

    void ProcessMovement(Vector2 moveDirection)
    {
        Vector2 currentPosition = transform.position;
        Vector2 movement = moveDirection * moveSpeed * Time.deltaTime;
        Vector2 newPosition = currentPosition + movement;

        if (!WillPositionIntersectWall(newPosition))
        {
            transform.position = newPosition;
            this.moveDirection = moveDirection;
            lastMoveDirection = moveDirection;
        }
        else
        {
            Debug.LogWarning("Prevented move into wall at final check!");
        }
    }

    bool WillPositionIntersectWall(Vector2 position)
    {
        if (playerCollider == null) return false;

        float radius = Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y) * 0.9f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, wallLayer);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Wall"))
            {
                return true;
            }
        }

        return false;
    }

    void OnZoomIn(InputValue value)
    {
        CameraZoom cameraZoom = FindFirstObjectByType<CameraZoom>();
        if (cameraZoom != null)
        {
            cameraZoom.IncreaseZoomLevel();
        }
    }

    void OnZoomOut(InputValue value)
    {
        CameraZoom cameraZoom = FindFirstObjectByType<CameraZoom>();
        if (cameraZoom != null)
        {
            cameraZoom.DecreaseZoomLevel();
        }
    }

    void OnBoost(InputValue value)
    {
        if (boostDuration > 0)
        {
            isBoost = true;
        }
        else
        {
            Debug.Log("No boost available!");
        }
    }

    void ProcessBoost()
    {
        if (canBoost && isBoost)
        {
            Boost();
            canBoost = false;
        }

        if (isBoost)
        {
            boostDuration -= Time.deltaTime;
            boostUI.SetBoost(boostDuration / maxBoostDuration);
            if (boostDuration <= 0)
            {
                isBoost = false;
                moveSpeed /= boostModifier;
                canBoost = true;
            }
        }
    }

    void Boost()
    {
        moveSpeed *= boostModifier;
        boostDuration -= Time.deltaTime;
    }

    private void RegenerateBoost()
    {
        if (!isBoost && boostDuration < maxBoostDuration)
        {
            boostDuration += Time.deltaTime;
            boostUI.SetBoost(boostDuration / maxBoostDuration);
        }
    }
}
