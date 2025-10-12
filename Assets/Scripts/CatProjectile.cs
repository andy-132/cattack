using UnityEngine;

public class CatProjectile : MonoBehaviour {
    // once we add more stuff, we should make it until it hits something (anything)
    [SerializeField] float maxLifetime = 6f;
    float life;

    void Update() {
        life += Time.deltaTime;
        if (life >= maxLifetime) Destroy(gameObject);
    }
}