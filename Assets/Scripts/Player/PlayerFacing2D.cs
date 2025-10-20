using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)] // run late so we win the transform fight
public class PlayerFacing2D_World : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoxCollider2D bodyCol;     // drag the player's real collider here (even if on a child)
    [SerializeField] private Transform throwOrigin;     // drag Player/ThrowOrigin
    [SerializeField] private SpriteRenderer sprite;     // optional: player visuals
    [SerializeField] private Camera cam;                // optional

    [Header("Offsets (world)")]
    [SerializeField] private float edgePadding = 0.08f; // extra beyond edge
    [SerializeField] private float yOffset = 0.10f;     // height above center

    [Header("Behavior")]
    [SerializeField] private bool faceMouseWhenIdle = true;

    public bool FacingRight { get; private set; } = true;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();

        // If bodyCol not assigned, try to find one anywhere under the player
        if (!bodyCol) bodyCol = GetComponentInChildren<BoxCollider2D>();

        if (!throwOrigin)
            Debug.LogWarning("PlayerFacing2D_World: throwOrigin not assigned.", this);
        if (!bodyCol)
            Debug.LogWarning("PlayerFacing2D_World: bodyCol not assigned (will use Renderer.bounds).", this);
    }

    void Update()
    {
        bool moved = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) { FacingRight = false; moved = true; }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) { FacingRight = true; moved = true; }
        }

        if (!moved && faceMouseWhenIdle && cam && Mouse.current != null)
        {
            float mx = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()).x;
            FacingRight = mx >= transform.position.x;
        }
    }

    void LateUpdate()
    {
        if (sprite) sprite.flipX = !FacingRight; // assumes art faces RIGHT by default
        if (!throwOrigin) return;

        // Use world bounds from collider OR renderer
        Bounds wb;
        if (bodyCol)         wb = bodyCol.bounds;
        else if (sprite)     wb = sprite.bounds;    // fallback
        else                 wb = new Bounds(transform.position, Vector3.one);

        float x = FacingRight ? (wb.max.x + edgePadding) : (wb.min.x - edgePadding);
        float y = wb.center.y + yOffset;

        throwOrigin.position = new Vector3(x, y, throwOrigin.position.z);

        // Debug line so you can SEE it in Scene view
        Debug.DrawLine(new Vector3(x, y, 0f), wb.center, Color.cyan);
    }

    public void FaceTowardX(float worldX) => FacingRight = worldX >= transform.position.x;
}