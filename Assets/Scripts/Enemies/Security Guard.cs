using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SecurityGuard : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;          // leave empty in prefab
    [SerializeField] Transform visual;          // child with SpriteRenderer
    [SerializeField] Transform attackPoint;
    [SerializeField] LayerMask playerLayer;     // set to "Player" in scene or via code

    [Header("Move")]
    [SerializeField] float moveSpeed = 4.5f;
    [SerializeField] float detectionRange = 8f;
    [SerializeField] float stopDistance = 1.2f;

    [Header("Attack")]
    [SerializeField] float attackRange = 0.8f;
    [SerializeField] float attackCooldown = 0.8f;
    [SerializeField] int attackDamage = 1;

    Rigidbody2D rb;
    float nextAttackTime;
    int faceDir = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // Auto-find player if not assigned (prefabs can't store scene refs)
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
            else
            {
                var ps = Object.FindAnyObjectByType<PlayerMovement>();
                if (ps) player = ps.transform;
            }
        }

        // Auto-fill playerLayer if not set
        if (playerLayer.value == 0)
            playerLayer = LayerMask.GetMask("Player");

        if (!visual) visual = transform; // fallback
    }

    public void SetTarget(Transform t) => player = t;   // allows spawner to assign

    void FixedUpdate()
    {
        if (!player) { rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); return; }

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        int desiredFace = dx >= 0 ? 1 : -1;
        if (Mathf.Abs(rb.linearVelocity.x) > 0.05f || dist > stopDistance + 0.1f)
            SetFacing(desiredFace);

        if (dist > detectionRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (dist > stopDistance)
            rb.linearVelocity = new Vector2(faceDir * moveSpeed, rb.linearVelocity.y);
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
        }
    }

    void SetFacing(int dir)
    {
        if (faceDir == dir) return;
        faceDir = dir;
        if (visual)
        {
            var ls = visual.localScale;
            ls.x = Mathf.Abs(ls.x) * faceDir;
            visual.localScale = ls;
        }
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        var hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (hit && hit.TryGetComponent<Health>(out var hp))
        {
            hp.TakeDamage(attackDamage);
            if (hit.attachedRigidbody)
            {
                Vector2 knock = new Vector2(3.8f * faceDir, 3f);
                var prb = hit.attachedRigidbody;
                prb.linearVelocity = new Vector2(knock.x, Mathf.Max(knock.y, prb.linearVelocity.y));
            }
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}