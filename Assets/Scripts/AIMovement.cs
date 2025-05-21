using UnityEngine;

public class AIMovement : MonoBehaviour
{

    [Header("Movement Settings")]
    [SerializeField] float minChangeInterval = 4f;
    [SerializeField] float maxChangeInterval = 8f;
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] LayerMask wallLayer;

    Vector2 moveDirection;
    public Vector2 MoveDirection { get { return moveDirection; } private set { moveDirection = value; } }

    Vector2 lastMoveDirection;
    float directionChangeTimer;
    float moveSpeed = 1f;

    void Start()
    {
        moveDirection = GetRandomDirection();
        lastMoveDirection = moveDirection;
        ResetTimer();
        if (wallLayer.value == 0)
        {
            wallLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("Wall Layer not set for Enemy - using Default layer");
        }

    }

    void Update()
    {
        directionChangeTimer -= Time.deltaTime;

        if (directionChangeTimer <= 0f)
        {
            moveDirection = ChooseNewDirection();
            ResetTimer();
        }

        if (!WallAhead(moveDirection))
        {
            MoveAI(moveDirection);
        }
        else
        {
            moveDirection = ChooseNewDirection();
        }
    }

        bool WallAhead(Vector2 direction)
        {
            Vector2 origin = (Vector2)transform.position;
            float checkDistance = wallCheckDistance;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, checkDistance, wallLayer);
            return hit.collider != null && hit.collider.CompareTag("Wall");
        }

        Vector2 ChooseNewDirection()
        {
            Vector2 newDirection;

            do
            {
                newDirection = GetRandomDirection();

            } while (newDirection == -lastMoveDirection);

            lastMoveDirection = newDirection;
            return newDirection;
        }

        Vector2 GetRandomDirection()
        {
            int randomDirection = Random.Range(0, 4);

            switch (randomDirection)
            {
                case 0: return Vector2.up;
                case 1: return Vector2.down;
                case 2: return Vector2.right;
                default: return Vector2.left;
            }
        }

    public void ChangeDirectionAwayFromWall(Vector2 currentDirection)
    {
        Vector2 newDirection = GetRandomDirection();
        // Ensure the new direction is not the opposite of the current direction
        if (newDirection == -currentDirection || newDirection == currentDirection)
        {
            newDirection = ChooseNewDirection();
        }
        moveDirection = newDirection;
        MoveAI(moveDirection);
    }

    void MoveAI(Vector2 moveDirection)
    {
        // Not respecting change to moveDirection if travelling in oposite direction
        Vector2 newPosition = (Vector2)transform.position + (moveDirection * moveSpeed * Time.deltaTime);
        transform.position = newPosition;
    }

    void ResetTimer()
    {
        directionChangeTimer = Random.Range(minChangeInterval, maxChangeInterval);
    }

}
