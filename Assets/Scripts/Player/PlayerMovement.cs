using UnityEngine;
using UnityEngine.InputSystem; // new Input System

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpForce = 20f;

    [Header("Grounding")]
    [SerializeField] LayerMask groundLayer; // set this in Inspector

    [Header("Visuals (optional)")]
    [SerializeField] Animator animator;              // drag your Animator here
    [SerializeField] SpriteRenderer spriteRenderer;  // drag if you want auto-flip

    Rigidbody2D rb;
    bool grounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = Mathf.Max(3f, rb.gravityScale);
        rb.freezeRotation = true;

        // Auto-fill references if missing
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // --- Horizontal input via new Input System ---
        float x = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

        // --- Apply move ---
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        // --- Jump (Space) ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (animator) animator.SetTrigger("IsJump"); // optional param
        }

        // --- Visuals / Animator ---
        if (spriteRenderer && Mathf.Abs(x) > 0.001f)
            spriteRenderer.flipX = (x < 0f); // assumes your sprite faces RIGHT by default

        if (animator)
        {
            bool running = Mathf.Abs(rb.linearVelocity.x) > 0.05f;
            animator.SetBool("IsRunning", running);      // bool parameter "Running"
        }
    }

    // --- Ground check via collisions with Ground layer ---
    void OnCollisionEnter2D(Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = true; }
    void OnCollisionStay2D (Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = true; }
    void OnCollisionExit2D (Collision2D c) { if (IsGround(c.gameObject.layer)) grounded = false; }

    bool IsGround(int layer) => (groundLayer.value & (1 << layer)) != 0;
}