using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]

public class PlayerMovement : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpForce = 20f;
    // do not forget to set the ground layer...
    [SerializeField] LayerMask groundLayer;
    Rigidbody2D rb;
    bool grounded;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = Mathf.Max(3f, rb.gravityScale);
        rb.freezeRotation = true;
    }

    void Update() {
        float x = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void OnCollisionEnter2D(Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = true; }
    void OnCollisionStay2D(Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = true; }
    void OnCollisionExit2D(Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = false; }
    bool IsGround(int layer) => (groundLayer.value & (1 << layer)) != 0;
}