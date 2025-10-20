using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class EnemySpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [SerializeField] private GameObject enemyPrefab;      // PREFAB asset (blue cube)
    [SerializeField] private LayerMask groundLayer;       // tick Ground

    [Header("Where to spawn")]
    [SerializeField] private bool useSpawnPoints = false;
    [SerializeField] private Transform[] spawnPoints;     // used if useSpawnPoints = true
    [SerializeField] private Vector2 randomXRange = new(-10f, 10f); // used if !useSpawnPoints
    [SerializeField] private float raycastTopY = 30f;     // start Y for down raycasts
    [SerializeField] private float yOffset = 0.05f;       // lift off ground a bit

    [Tooltip("Check this radius to avoid overlapping other colliders at spawn")]
    [SerializeField] private float clearRadius = 0.4f;
    [SerializeField] private LayerMask blockLayersAtSpawn = ~0; // what counts as 'occupied' (default: everything)

    [Header("When to spawn")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int   burstCount = 1;

    [Header("Difficulty ramp")]
    [SerializeField] private bool  rampDifficulty = false;
    [SerializeField] private float rampEverySeconds = 20f;
    [SerializeField] private int   maxBurst = 8;

    [Header("Retries / Debug")]
    [SerializeField] private int groundRayMaxTries = 8;    // try new X up to N times
    [SerializeField] private int clearSpotMaxTries = 6;    // nudge X to find a clear spot
    [SerializeField] private bool verboseLogs = false;
    [SerializeField] private bool drawRays = true;

    void Start()
    {
        if (!enemyPrefab)
        {
            Debug.LogError("EnemySpawner: enemyPrefab not assigned (drag a PREFAB asset).", this);
            enabled = false; return;
        }
        StartCoroutine(SpawnLoop());
        if (rampDifficulty) StartCoroutine(RampLoop());
    }

    IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            SpawnBurst(burstCount);
            yield return wait;
        }
    }

    IEnumerator RampLoop()
    {
        var wait = new WaitForSeconds(rampEverySeconds);
        while (true)
        {
            yield return wait;
            if (burstCount < maxBurst) burstCount++;
            if (verboseLogs) Debug.Log($"[Spawner] Ramp → burstCount = {burstCount}", this);
        }
    }

    void SpawnBurst(int count)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            if (TryGetSpawnPosition(out Vector3 pos))
            {
                pos.z = 0f; // ensure on camera plane
                var obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
                spawned++;
                if (verboseLogs) Debug.Log($"[Spawner] Spawned at {pos}", obj);
            }
            else if (verboseLogs)
            {
                Debug.LogWarning("[Spawner] Could not find valid ground/clear spot.", this);
            }
        }
        if (verboseLogs && spawned == 0) Debug.LogWarning("[Spawner] Burst produced 0 spawns.", this);
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        // Try multiple X candidates to find ground
        for (int tries = 0; tries < groundRayMaxTries; tries++)
        {
            float x = ChooseX();
            Vector2 start = new Vector2(x, raycastTopY);

            if (drawRays)
                Debug.DrawRay(start, Vector2.down * 200f, Color.yellow, 0.5f);

            RaycastHit2D hit = Physics2D.Raycast(start, Vector2.down, Mathf.Infinity, groundLayer);
            if (!hit.collider) continue; // miss: try another X

            // Candidate point on ground
            Vector3 candidate = hit.point + Vector2.up * yOffset;

            // Try to find a clear spot by nudging side-to-side
            if (FindClearNearby(ref candidate))
            {
                pos = candidate;
                return true;
            }
        }

        pos = default;
        return false;
    }

    bool FindClearNearby(ref Vector3 candidate)
    {
        // center + alternating nudges
        float[] nudges = { 0f, 0.6f, -0.6f, 1.2f, -1.2f, 1.8f };
        int attempts = Mathf.Min(clearSpotMaxTries, nudges.Length);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 test = candidate;
            test.x += nudges[i];

            bool blocked = Physics2D.OverlapCircle(test, clearRadius, blockLayersAtSpawn) != null;
            if (!blocked)
            {
                candidate = test;
                return true;
            }
        }
        return false;
    }

    float ChooseX()
    {
        if (useSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform p = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return p ? p.position.x : transform.position.x;
        }

        // auto-fix reversed ranges
        float min = Mathf.Min(randomXRange.x, randomXRange.y);
        float max = Mathf.Max(randomXRange.x, randomXRange.y);
        return Random.Range(min, max);
    }

    [ContextMenu("Spawn One Here")]
    void SpawnOneHere()
    {
        Vector3 here = transform.position; here.z = 0f;
        var obj = Instantiate(enemyPrefab, here, Quaternion.identity);
        if (verboseLogs) Debug.Log($"[Spawner] Manual spawn at {here}", obj);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (!useSpawnPoints)
        {
            float min = Mathf.Min(randomXRange.x, randomXRange.y);
            float max = Mathf.Max(randomXRange.x, randomXRange.y);
            Vector3 a = new Vector3(min, transform.position.y, 0);
            Vector3 b = new Vector3(max, transform.position.y, 0);
            Gizmos.DrawLine(a, b);
        }
        else if (spawnPoints != null)
        {
            foreach (var p in spawnPoints) if (p) Gizmos.DrawSphere(p.position, 0.1f);
        }
    }
}