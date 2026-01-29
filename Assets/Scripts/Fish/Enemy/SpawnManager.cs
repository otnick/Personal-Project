using UnityEngine;

[System.Serializable]
public class EnemyEntry
{
    public GameObject prefab;
    public float spawnRate = 1f;
}

public class SpawnManager : MonoBehaviour
{
    public EnemyEntry[] enemyEntries;

    [Header("Spawnbereich (3D-Sphäre um ein zentrales Empty)")]
    public Transform spawnCenter;    // <--- festes Empty in der Szene
    public float minSpawnRadius = 8f;
    public float maxSpawnRadius = 20f;

    public float spawnsPerMinute = 30f;

    float acc;

    void Start()
    {
        SpawnOne();
    }

    void Update()
    {
        if (spawnCenter == null) return; // kein Spieler nötig mehr
        if (spawnCenter.GetComponent<Damageable>()?.currentHealth <= 0f) return;

        acc += spawnsPerMinute / 60f * Time.deltaTime;
        while (acc >= 1f)
        {
            SpawnOne();
            acc -= 1f;
        }
    }

    void SpawnOne()
    {
        if (spawnCenter == null) return;
        if (enemyEntries == null || enemyEntries.Length == 0) return;

        // weighted selection
        float total = 0f;
        foreach (var e in enemyEntries)
            total += Mathf.Max(0.0001f, e.spawnRate);

        float r = Random.value * total;

        foreach (var e in enemyEntries)
        {
            r -= Mathf.Max(0.0001f, e.spawnRate);
            if (r <= 0f)
            {
                Vector3 centerPos = spawnCenter.position;
                Vector3 pos = centerPos;
                int safety = 30;

                for (int i = 0; i < safety; i++)
                {
                    Vector3 dir = Random.onUnitSphere;
                    float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
                    Vector3 candidate = centerPos + dir * radius;

                    // optionaler Mindestabstand (redundant hier, aber kann bleiben)
                    if (Vector3.Distance(centerPos, candidate) >= minSpawnRadius)
                    {
                        pos = candidate;
                        break;
                    }
                }

                GameObject instance = Instantiate(e.prefab, pos, Quaternion.identity);

                // dem FishAI das feste Zentrum übergeben (falls vorhanden)
                var fishAI = instance.GetComponent<FishAI>();
                if (fishAI != null)
                {
                    fishAI.center = spawnCenter;
                }

                return;
            }
        }
    }
}
