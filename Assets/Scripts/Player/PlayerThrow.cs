using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PlayerThrow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform throwOrigin;          // optional; used for Z or fallback
    [SerializeField] private GameObject projectilePrefab;    // prefab asset (NOT scene object)
    [SerializeField] private Camera cam;

    [Header("Spawn offset from player body")]
    [SerializeField] private BoxCollider2D playerBody;       // player collider (for bounds)
    [SerializeField] private float spawnEdgePadding = 0.06f; // how far outside the edge
    [SerializeField] private float spawnYOffset     = 0.10f; // hand height above center

    [Header("Light Throw (LMB)")]
    [SerializeField] private float lightThrowSpeed = 14f;
    [SerializeField] private float throwCooldown   = 0.08f;

    [Header("Heavy Throw (RMB)")]
    [SerializeField] private float chargedMinSpeed = 8f;
    [SerializeField] private float chargedMaxSpeed = 28f;
    [SerializeField] private float maxChargeTime   = 3f;

    [Header("Ammo")]
    [SerializeField] private int   maxAmmo      = 8;
    [SerializeField] private float reloadSpeed  = 0.5f;  // +1 ammo every 0.5s
    private int ammo;
    private float nextAmmoRegenTime;

    [Header("Ammo Bar (sprite over head)")]
    [SerializeField] private Transform ammoBarRoot;  // parent
    [SerializeField] private Transform ammoBarFill;  // scale X
    [SerializeField] private float     barFullWidth = 0.8f;

    [Header("Animation")]
    [SerializeField] private Animator animator;                 // drag Player Animator (or auto)
    [SerializeField] private string   lightThrowTrig = "LightThrow";
    [SerializeField] private string   heavyThrowTrig = "HeavyThrowTrig"; // optional
    [SerializeField] private string   chargingBool   = "Charging";   // bool for hold loop

    // state
    private bool  charging = false;
    private float chargeTimer = 0f;
    private float lastThrowTime = -999f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!animator) animator = GetComponentInChildren<Animator>();

        ammo = maxAmmo;
        nextAmmoRegenTime = Time.time + reloadSpeed;

        if (ammoBarRoot) ammoBarRoot.gameObject.SetActive(true);
        UpdateAmmoBar();

        // sanity check prefab reference
        if (projectilePrefab != null && projectilePrefab.scene.IsValid())
            Debug.LogError("PlayerThrow: 'projectilePrefab' is a SCENE object. Drag the PREFAB asset from Project.", this);
    }

    void Update()
    {
        // --- ammo regen ---
        if (ammo < maxAmmo && Time.time >= nextAmmoRegenTime)
        {
            ammo++;
            nextAmmoRegenTime = Time.time + reloadSpeed;
            UpdateAmmoBar();
        }

        // --- light throw ---
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryLightThrow();

        // --- heavy start (press) ---
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            charging = true;
            chargeTimer = 0f;
            SetChargingAnim(true);
        }

        // --- heavy charging (hold) ---
        if (charging && Mouse.current.rightButton.isPressed)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer > maxChargeTime) chargeTimer = maxChargeTime;
        }

        // --- heavy release ---
        if (charging && (Mouse.current.rightButton.wasReleasedThisFrame ||
                         (!Mouse.current.rightButton.isPressed && chargeTimer > 0f)))
        {
            DoChargedThrow();
        }
    }

    // ================== actions ==================
    void TryLightThrow()
    {
        if (!CanFire()) return;

        Vector2 dir = AimDirection();

        // play throw animation
        if (animator && !string.IsNullOrEmpty(lightThrowTrig))
            animator.SetTrigger(lightThrowTrig);

        Fire(dir * lightThrowSpeed);
    }

    void DoChargedThrow()
    {
        // always stop Charging anim
        SetChargingAnim(false);

        if (!CanFire())
        {
            charging = false;
            chargeTimer = 0f;
            return;
        }

        float t = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float speed = Mathf.Lerp(chargedMinSpeed, chargedMaxSpeed, t);
        Vector2 dir = AimDirection();

        if (animator && !string.IsNullOrEmpty(heavyThrowTrig))
            animator.SetTrigger(heavyThrowTrig);

        Fire(dir * speed);

        charging = false;
        chargeTimer = 0f;
    }

    // ================== helpers ==================
    bool CanFire()
    {
        if (Time.time - lastThrowTime < throwCooldown) return false;
        if (ammo <= 0) return false;
        if (projectilePrefab == null || projectilePrefab.scene.IsValid()) return false;
        return true;
    }

    void Fire(Vector2 initialVelocity)
    {
        lastThrowTime = Time.time;
        ammo = Mathf.Max(0, ammo - 1);
        UpdateAmmoBar();

        // spawn just outside player on aim side
        Vector2 dir = initialVelocity.normalized;
        Vector3 spawnPos = ComputeSpawnPosition(dir);

        GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // ignore collisions with player colliders
        var playerCols = GetComponentsInChildren<Collider2D>();
        var projCols   = obj.GetComponentsInChildren<Collider2D>();
        foreach (var pc in projCols)
            foreach (var pl in playerCols)
                if (pc && pl) Physics2D.IgnoreCollision(pc, pl, true);

        if (obj.TryGetComponent<Rigidbody2D>(out var prb))
            prb.linearVelocity = initialVelocity;
    }

    Vector3 ComputeSpawnPosition(Vector2 aimDir)
    {
        if (!playerBody)
            return (throwOrigin ? throwOrigin.position : transform.position) + (Vector3)(aimDir * spawnEdgePadding);

        Bounds b = playerBody.bounds; // world space
        bool right = aimDir.x >= 0f;
        float x = right ? (b.max.x + spawnEdgePadding) : (b.min.x - spawnEdgePadding);
        float y = b.center.y + spawnYOffset;
        float z = throwOrigin ? throwOrigin.position.z : 0f;
        return new Vector3(x, y, z);
    }

    Vector2 AimDirection()
    {
        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = cam ? cam.ScreenToWorldPoint(mouseScreen) : Vector3.zero;
        mouseWorld.z = 0f;

        // optional: face the mouse horizontally
        var face = GetComponent<PlayerFacing2D_World>();
        if (face) face.FaceTowardX(mouseWorld.x);

        Vector2 dir = (mouseWorld - transform.position);
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
        return dir.normalized;
    }

    void UpdateAmmoBar()
    {
        if (!ammoBarRoot || !ammoBarFill || maxAmmo <= 0) return;

        float pct = Mathf.Clamp01((float)ammo / maxAmmo);
        ammoBarFill.localScale = new Vector3(pct, 0.12f, 1f);
        ammoBarFill.localPosition = new Vector3(
            -(1f - pct) * barFullWidth * 0.5f,
            ammoBarFill.localPosition.y,
            ammoBarFill.localPosition.z
        );
    }

    void SetChargingAnim(bool on)
    {
        if (animator && !string.IsNullOrEmpty(chargingBool))
            animator.SetBool(chargingBool, on);
    }

    void OnDisable() => SetChargingAnim(false);
}