using UnityEngine;

public class Highlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] float outlineWidth = 0.05f;
    [SerializeField] Sprite whiteOutlineSprite;


    Color smallerEntityColor = Color.green;
    Color largerEntityColor = Color.red;

    // Reference to the main player
    GameObject player;
    PlayerSize playerSize;

    // Reference to this entity's size
    PlayerSize mySize;

    // Outline references
    GameObject outlineObject;
    SpriteRenderer outlineRenderer;
    SpriteRenderer spriteRenderer;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
            return;
        }

        playerSize = player.GetComponent<PlayerSize>();
        mySize = GetComponent<PlayerSize>();

        if (mySize == null)
        {
            Debug.LogError("This GameObject doesn't have a PlayerSize component!" + this.name);
            return;
        }

        SetupOutline();
        UpdateOutlineColor();
    }

    void Update()
    {
        // Change to update when size changes instead of each frame
        UpdateOutlineColor();
    }

    void SetupOutline()
    {
        if (whiteOutlineSprite == null)
        {
            Debug.LogError("No outline sprite assigned!");
            return;
        }

        // Get or add sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this GameObject!");
            return;
        }

        // Create a child GameObject for the outline
        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        // Add sprite renderer to the outline object
        outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();

        // Copy the sprite from the parent
        outlineRenderer.sprite = whiteOutlineSprite;

        // Ensure outline is drawn behind the original sprite
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        // Make the outline slightly larger
        outlineObject.transform.localScale = new Vector3(
            1 + outlineWidth,
            1 + outlineWidth,
            1
        );
    }

    void UpdateOutlineColor()
    {
        if (playerSize == null || mySize == null || outlineRenderer == null) return;

        outlineRenderer.color = (mySize.playerSize < playerSize.playerSize)
            ? smallerEntityColor
            : largerEntityColor;
    }
}
