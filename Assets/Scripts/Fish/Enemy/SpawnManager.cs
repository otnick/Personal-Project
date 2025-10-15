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
    public Vector2 areaX = new Vector2(-20, 20);
    public Vector2 areaY = new Vector2(-8, 8);
    public GameObject player;
    public float spawnsPerMinute = 30f;
    float acc;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnOne();
    }

    // Update is called once per frame
    void Update()
    {
        if (!player) return;
        if (player.GetComponent<Damageable>()?.currentHealth <= 0f) return;
        acc += spawnsPerMinute / 60f * Time.deltaTime;
        while (acc >= 1f) { SpawnOne(); acc -= 1f; }
    }
    
    void SpawnOne()
    {
        if (enemyEntries == null || enemyEntries.Length == 0) return;

        // gewichtete Auswahl
        float total = 0f;
        foreach (var e in enemyEntries) total += Mathf.Max(0.0001f, e.spawnRate);
        float r = Random.value * total;

        foreach (var e in enemyEntries)
        {
            r -= Mathf.Max(0.0001f, e.spawnRate);
            if (r <= 0f)
            {
                Vector3 playerPos = player.transform.position;
                Vector3 pos = Vector3.zero;
                float minDistance = 8f;   // <- Gegner dürfen nicht näher als 8 Einheiten spawnen
                int safety = 30;          // <- maximal 30 Versuche, um Endlosschleifen zu vermeiden

                for (int i = 0; i < safety; i++)
                {
                    // Zufälligen Punkt rund um den Spieler
                    Vector3 offset = new Vector3(Random.Range(areaX.x, areaX.y),
                                                Random.Range(areaY.x, areaY.y), 0f);
                    pos = playerPos + offset;

                    // Prüfen: weit genug weg?
                    if (Vector3.Distance(playerPos, pos) >= minDistance)
                        break;
                }

                pos.z = 0f;
                Instantiate(e.prefab, pos, Quaternion.identity);
                return;
            }
        }
    }
}
