using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CatProjectile : MonoBehaviour {
    [SerializeField] float maxLifetime = 6f;
    [SerializeField] int damage = 1;

    [Header("Hit Filtering (LayerMask)")]
    [SerializeField] LayerMask enemyLayers; // tick "Enemy" in Inspector

    float life;

    void Awake() {
        // Safety: ensure we're not tunneling
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update() {
        life += Time.deltaTime;
        if (life >= maxLifetime) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D c) {
        TryDamage(c.collider);
        Destroy(gameObject);
    }

    void TryDamage(Collider2D col) {
        bool inEnemyMask = (enemyLayers.value & (1 << col.gameObject.layer)) != 0;
        var hp = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();

        // DEBUG: uncomment while testing
        // Debug.Log($"Hit {col.name} | layer={LayerMask.LayerToName(col.gameObject.layer)} | inEnemyMask={inEnemyMask} | hp={(hp!=null)}");

        if (inEnemyMask && hp != null)
            hp.TakeDamage(damage);
    }
}