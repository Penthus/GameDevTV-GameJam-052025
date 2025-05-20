using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            if(this.CompareTag("Player"))
            {
                // Assuming the player has a PlayerMovement script attached
                PlayerMovement playerMovement = this.GetComponent<PlayerMovement>();
                Vector2 currentDirection = playerMovement.MoveDirection;
                Debug.Log("Player collided with wall. Current direction: " + currentDirection);

                // Determine the wall normal direction (opposite of player's current direction)
                Vector2 wallNormal = new Vector2(-currentDirection.x, -currentDirection.y);

                // Calculate perpendicular directions (left and right along the wall)
                Vector2 leftAlongWall = new Vector2(-wallNormal.y, wallNormal.x).normalized;
                Vector2 rightAlongWall = new Vector2(wallNormal.y, -wallNormal.x).normalized;

                // Make sure we're using cardinal directions (up, down, left, right)
                if (Mathf.Abs(leftAlongWall.x) > Mathf.Abs(leftAlongWall.y))
                {
                    leftAlongWall = new Vector2(Mathf.Sign(leftAlongWall.x), 0);
                    rightAlongWall = new Vector2(-Mathf.Sign(leftAlongWall.x), 0);
                }
                else
                {
                    leftAlongWall = new Vector2(0, Mathf.Sign(leftAlongWall.y));
                    rightAlongWall = new Vector2(0, -Mathf.Sign(leftAlongWall.y));
                }

                // Randomly choose between left and right along the wall
                Vector2 newDirection;
                if (UnityEngine.Random.value < 0.5f)
                {
                    newDirection = leftAlongWall;
                    Debug.Log("Choosing to move left along the wall");
                }
                else
                {
                    newDirection = rightAlongWall;
                    Debug.Log("Choosing to move right along the wall");
                }

                // Make sure we don't double back on ourselves
                if ((newDirection + playerMovement.MoveDirection) == Vector2.zero)
                {
                    // If we somehow picked the opposite direction, use the other option
                    newDirection = (newDirection == leftAlongWall) ? rightAlongWall : leftAlongWall;
                    Debug.Log("Corrected to avoid doubling back. New direction: " + newDirection);
                }

                // Update the player's direction
                playerMovement.ChangeDirection(newDirection);
                Debug.Log("Player changed direction after wall collision. New direction = " + newDirection);
            }
            else if (this.CompareTag("Enemy"))
            {
                Debug.Log("Enemy AI collided with wall.");
                AIMovement AIMovement = this.gameObject.GetComponent<AIMovement>();
                Vector2 currentDirection = AIMovement.MoveDirection;
                AIMovement.ChangeDirectionAwayFromWall(currentDirection);
                Debug.Log("Enemy AI changed direction after wall collision.");
            }
            
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision detected with: " + other.name);
        if (other.CompareTag("Player"))
        {
            if (this.CompareTag("Food"))
            {
                // Assuming the player has a PlayerSize script attached
                PlayerSize playerSize = other.GetComponent<PlayerSize>();
                playerSize.IncreaseSize();
                Destroy(gameObject);
            }
            else if (this.CompareTag("Player"))
            {
                // Handle player collision with another player
                PlayerSize otherPlayerSize = other.GetComponent<PlayerSize>();
                if (otherPlayerSize != null)
                {
                    // Assuming you want to handle player collision logic here
                    Debug.Log("Collision between players detected.");
                }
            }
            else if (this.CompareTag("Enemy"))
            {
                // Handle player collision with an obstacle
                Debug.Log("Collision with Enemy AI detected.");
                PlayerSize otherPlayerSize = other.GetComponent<PlayerSize>();
                PlayerSize playerSize = this.GetComponent<PlayerSize>();

                if (playerSize.playerSize > otherPlayerSize.playerSize)
                {                    
                    playerSize.IncreaseSize(otherPlayerSize.playerSize);
                    Debug.Log("Player got eaten by larger enemy AI.");
                    Destroy(other.gameObject);
                }
                else
                {
                    otherPlayerSize.IncreaseSize(playerSize.playerSize);
                    Debug.Log("Enemy got eaten by larger Player.");
                    Destroy(this.gameObject);
                }
            }

            else
            {
                { // Handle other collisions if needed
                    Debug.Log("Collision with non-player tagged object: " + other.name);
                }
            }
        }
    }
}
