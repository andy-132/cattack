using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFacing2D_World : MonoBehaviour {
    [Header("Refs")]
    [SerializeField] private BoxCollider2D bodyCol;   // Player BoxCollider2D
    [SerializeField] private Transform throwOrigin;   // spawn point (can be child or not)
    [SerializeField] private SpriteRenderer sprite;   // optional: to flip visuals
    [SerializeField] private Camera cam;              // optional

    [Header("Offsets (world units)")]
    [SerializeField] private float edgePadding = 0.08f; // extra beyond edge
    [SerializeField] private float yOffset = 0.10f;     // height above center

    [Header("Behavior")]
    [SerializeField] private bool faceMouseWhenIdle = true;

    public bool FacingRight { get; private set; } = true;

    void Awake() {
        if (!cam) cam = Camera.main;
        if (!bodyCol) bodyCol = GetComponent<BoxCollider2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Update() {
        bool moved = false;

        // Keyboard sets facing
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) { FacingRight = false; moved = true; }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) { FacingRight = true; moved = true; }
        }

        // Idle → face mouse X
        if (!moved && faceMouseWhenIdle && cam)
        {
            var mx = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()).x;
            FacingRight = mx >= transform.position.x;
        }
    }

    void LateUpdate() {
        if (sprite) sprite.flipX = !FacingRight; // assumes art faces RIGHT by default

        if (!throwOrigin || !bodyCol) return;

        // Use WORLD bounds so scaling/parents don’t matter
        Bounds b = bodyCol.bounds; // world space
        float x = FacingRight ? (b.max.x + edgePadding) : (b.min.x - edgePadding);
        float y = b.center.y + yOffset;

        // Physically place the ThrowOrigin in world space
        throwOrigin.position = new Vector3(x, y, throwOrigin.position.z);
    }

    // Call this from PlayerThrow before firing if you want aim to also flip
    public void FaceTowardX(float worldX)
    {
        FacingRight = worldX >= transform.position.x;
    }
}