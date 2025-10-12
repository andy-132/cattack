using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrow : MonoBehaviour {
    [Header("Refs")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Camera cam;

    [Header("Light Throw (LMB)")]
    [SerializeField] private float lightThrowSpeed = 14f;
    [SerializeField] private float throwCooldown = 0.08f;

    [Header("Heavy Throw (RMB)")]
    [SerializeField] private float chargedMinSpeed = 8f;
    [SerializeField] private float chargedMaxSpeed = 28f;
    [SerializeField] private float maxChargeTime = 3f;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 8;
    [SerializeField] private float reloadSpeed = 0.5f;
    private int ammo;
    private float nextAmmoRegenTime;

    [Header("Ammo Bar (sprite-based UI)")]
    [SerializeField] private Transform ammoBarRoot;
    [SerializeField] private Transform ammoBarFill;
    [SerializeField] private float barFullWidth = 0.8f;

    // playerstate
    private bool charging = false;
    private float chargeTimer = 0f;
    private float lastThrowTime = -999f;

    void Awake() {
        if (!cam) cam = Camera.main;
        ammo = maxAmmo;
        nextAmmoRegenTime = Time.time + reloadSpeed;
        if (ammoBarRoot) ammoBarRoot.gameObject.SetActive(true);
        UpdateAmmoBar();
    }

    void Update() {
        // ammo regen 
        if (ammo < maxAmmo && Time.time >= nextAmmoRegenTime) {
            ammo++;
            nextAmmoRegenTime = Time.time + reloadSpeed;
            UpdateAmmoBar();
        }

        // light throw
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            TryLightThrow();
        }

        // heavy attack, we should have another bar for this, maybe 
        // ammo can be at the bottom of screen
        if (Mouse.current.rightButton.wasPressedThisFrame) {
            charging = true;
            chargeTimer = 0f;
        }

        // INSANEPITCHBUILDUPCALCULATION
        if (charging && Mouse.current.rightButton.isPressed) {
            chargeTimer += Time.deltaTime;
            if (chargeTimer > maxChargeTime) chargeTimer = maxChargeTime;
        }

        // release to toss
        if (charging && (Mouse.current.rightButton.wasReleasedThisFrame ||
             (!Mouse.current.rightButton.isPressed && chargeTimer > 0f))) {
            DoChargedThrow();
        }
    }

    void TryLightThrow() {
        if (!CanFire()) return;
        Vector2 dir = AimDirection();
        Fire(dir * lightThrowSpeed);
    }

    void DoChargedThrow() {
        if (!CanFire()) {
            charging = false;
            chargeTimer = 0f;
            return;
        }

        float t = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float speed = Mathf.Lerp(chargedMinSpeed, chargedMaxSpeed, t);
        Vector2 dir = AimDirection();
        Fire(dir * speed);

        charging = false;
        chargeTimer = 0f;
    }

    bool CanFire() {
        if (Time.time - lastThrowTime < throwCooldown) return false;
        if (ammo <= 0) return false;
        if (projectilePrefab == null || projectilePrefab.scene.IsValid()) return false;
        return true;
    }

    void Fire(Vector2 initialVelocity) {
        lastThrowTime = Time.time;
        ammo = Mathf.Max(0, ammo - 1);
        UpdateAmmoBar();

        GameObject obj = Instantiate(projectilePrefab, throwOrigin.position, Quaternion.identity);
        if (obj.TryGetComponent<Rigidbody2D>(out var prb)) {
            prb.linearVelocity = initialVelocity;
        }
    }

    Vector2 AimDirection() {
        // if we can also have the parabola of aim that would be sick asf
        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = cam ? cam.ScreenToWorldPoint(mouseScreen) : Vector3.zero;
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - throwOrigin.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        return dir.normalized;
    }

    void UpdateAmmoBar() {
        if (!ammoBarRoot || !ammoBarFill || maxAmmo <= 0) return;

        float pct = Mathf.Clamp01((float)ammo / maxAmmo);
        // scale x by (0..1)
        // for some reason it moves a lil to the right too :(
        ammoBarFill.localScale = new Vector3(pct, 0.12f, 1f);

        // shift pos left so in theory the edge is fixed 
        ammoBarFill.localPosition = new Vector3(
            -(1f - pct) * barFullWidth * 0.5f,
            ammoBarFill.localPosition.y,
            ammoBarFill.localPosition.z
        );
    }
}