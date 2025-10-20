using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] int maxHP = 3;
    public int Current { get; private set; }

    public System.Action OnDeath;

    DamageFlash flash;  // optional

    void Awake()
    {
        Current = maxHP;
        flash = GetComponentInChildren<DamageFlash>() ?? GetComponent<DamageFlash>();
    }

    public void TakeDamage(int dmg)
    {
        if (Current <= 0) return;
        Current -= Mathf.Max(1, dmg);
        flash?.DoFlash();                // <-- flash on hit
        if (Current <= 0) Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}