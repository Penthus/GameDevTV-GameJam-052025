using System;
using System.Collections.Generic;
using System.Linq;
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

    // Store recent wall collisions to prevent moving into them
    private Dictionary<string, float> recentWallCollisions = new Dictionary<string, float>();
    private float wallCollisionCooldown = 0.5f; // Time to remember wall collisions

    Vector2 moveDirection;
    public Vector2 MoveDirection { get { return moveDirection; } private set { moveDirection = value; } }

    Vector2 lastMoveDirection;
    float boostDuration = 2f;
    bool canBoost = true;
    bool isBoost = false;
    Collider2D playerCollider;

    // For visual debugging
    private bool hasLoggedWallIssue = false;

    void Start()
    {
        moveDirection = Vector2.up;
        lastMoveDirection = Vector2.up;
        playerCollider = GetComponent<Collider2D>();

        // If no wall layer is set, use default
        if (wallLayer.value == 0)
        {
            wallLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("Wall Layer not set - using Default layer");
        }
    }

    void Update()
    {
        // Create a copy of the keys to iterate over
        string[] wallIds = recentWallCollisions.Keys.ToArray();

        // Update recent wall collision timers
        List<string> expiredCollisions = new List<string>();
        foreach (var wallId in wallIds)
        {
            recentWallCollisions[wallId] -= Time.deltaTime;
            if (recentWallCollisions[wallId] <= 0)
                expiredCollisions.Add(wallId);
        }

        // Clean up expired wall collisions
        foreach (var wallId in expiredCollisions)
            recentWallCollisions.Remove(wallId);

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

            // Remember this wall to avoid it
            string wallId = overlapCollider.GetInstanceID().ToString();
            if (!recentWallCollisions.ContainsKey(wallId))
                recentWallCollisions.Add(wallId, wallCollisionCooldown);

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

        List<Collider2D> results = new List<Collider2D>();
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
        // Distance from player to closest point on wall
        float currentDistance = Vector2.Distance(playerPosition, closestPointOnWall);

        // Player radius (use the smaller of width/height for conservative estimate)
        float playerRadius = Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y);

        // Calculate how far to push out (should be at least the player's radius)
        float pushDistance = (playerRadius - currentDistance) + safetyMargin;

        // Only push if we need to
        if (pushDistance > 0)
        {
            // Move player out of the wall
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

    private void RegenerateBoost()
    {
        if (!isBoost && boostDuration < maxBoostDuration)
        {
            boostDuration += Time.deltaTime;
        }
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
            // Calculate the position this input would lead to
            Vector2 nextPosition = CalculateNextPosition(input);

            // Check if this direction is valid (no wall ahead AND no collision at next position)
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
        Vector2 origin = (Vector2)transform.position - (direction * 0.1f); // Start slightly behind to catch walls we're close to
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance + 0.1f, wallLayer);

        // Debug ray
        Debug.DrawRay(origin, direction * (wallCheckDistance + 0.1f), hit.collider != null ? Color.red : Color.green, 0.1f);

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            // Remember this wall
            string wallId = hit.collider.GetInstanceID().ToString();
            if (!recentWallCollisions.ContainsKey(wallId))
                recentWallCollisions.Add(wallId, wallCollisionCooldown);

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
            // Remember this wall
            string wallId = hit.collider.GetInstanceID().ToString();
            if (!recentWallCollisions.ContainsKey(wallId))
                recentWallCollisions.Add(wallId, wallCollisionCooldown);

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

        // Check both perpendicular directions
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
            // Try opposite direction as last resort
            Vector2 opposite = -moveDirection;
            if (!WallAhead(opposite) && !WillPositionIntersectWall(CalculateNextPosition(opposite)))
            {
                moveDirection = opposite;
                lastMoveDirection = moveDirection;
                ProcessMovement(moveDirection);
            }
            else
            {
                // All directions are blocked, don't move
                Debug.Log("Surrounded by walls, cannot move!");
            }
        }
    }

    public void ChangeDirection(Vector2 newDirection)
    {
        //ProcessMovement(newDirection);

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

        // Final safety check to prevent moving into walls
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

        // Use overlap circle with slightly smaller radius for collision prediction
        float radius = Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y) * 0.9f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, wallLayer);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Wall"))
            {
                string wallId = collider.GetInstanceID().ToString();
                if (!recentWallCollisions.ContainsKey(wallId))
                    recentWallCollisions.Add(wallId, wallCollisionCooldown);

                return true;
            }
        }

        // Check against recently collided walls for extra safety
        foreach (var wallId in recentWallCollisions.Keys)
        {
            Collider2D wallCollider = GetColliderFromInstanceID(wallId);
            if (wallCollider != null && WillCollideWithWall(position, wallCollider))
                return true;
        }

        return false;
    }

    Collider2D GetColliderFromInstanceID(string instanceId)
    {
        int id;
        if (int.TryParse(instanceId, out id))
        {
            // Instead of using FindObjectFromInstanceID, we'll use a different approach
            // to find the collider by tag and compare its instance ID
            Collider2D[] wallColliders = GameObject.FindGameObjectsWithTag("Wall")
                .Select(wall => wall.GetComponent<Collider2D>())
                .Where(collider => collider != null)
                .ToArray();

            foreach (Collider2D collider in wallColliders)
            {
                if (collider.GetInstanceID().ToString() == instanceId)
                    return collider;
            }
        }
        return null;
    }

    bool WillCollideWithWall(Vector2 position, Collider2D wallCollider)
    {
        // Special check for specific wall we've recently collided with
        if (playerCollider == null || wallCollider == null) return false;

        bool result = false;

        try
        {
            // Use a simple distance check as fallback
            float distance = Vector2.Distance(position, wallCollider.bounds.ClosestPoint(position));
            float minDistance = playerCollider.bounds.extents.magnitude * 0.9f;
            result = distance < minDistance;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking wall collision: {e.Message}");
            result = false;
        }

        return result;
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
            if (boostDuration <= 0)
            {
                isBoost = false;
                moveSpeed /= boostModifier;
                Debug.Log("Boost ended!");
                canBoost = true;
            }
        }

    }
    void Boost()
    {
        // Implement boost logic here
        Debug.Log("Boost activated!");
        moveSpeed *= boostModifier;
        boostDuration -= Time.deltaTime;
    }

    // Called when this object collides with a wall
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Remember this wall to avoid it
            string wallId = collision.collider.GetInstanceID().ToString();
            if (!recentWallCollisions.ContainsKey(wallId))
                recentWallCollisions.Add(wallId, wallCollisionCooldown);

            Debug.Log($"Detected wall collision in OnCollisionEnter2D: {collision.collider.name}");
        }
    }

    // Draw gizmos to help visualize collision detection
    void OnDrawGizmos()
    {
        if (playerCollider == null) return;

        // Draw the player's collision radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position,
            Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y) * 0.9f);

        // Draw wall check rays in all four directions
        Gizmos.color = Color.cyan;
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        foreach (Vector2 dir in directions)
        {
            Vector2 origin = (Vector2)transform.position - (dir * 0.1f);
            Gizmos.DrawLine(origin, origin + dir * (wallCheckDistance + 0.1f));
        }
    }
}