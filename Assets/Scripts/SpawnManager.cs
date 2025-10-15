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
        acc += spawnsPerMinute / 60f * Time.deltaTime;
        while (acc >= 1f) { SpawnOne(); acc -= 1f; }
    }
    
    void SpawnOne()
    {
        if (enemyEntries == null || enemyEntries.Length == 0) return;
        // gewichtete Auswahl
        float total = 0f; foreach (var e in enemyEntries) total += Mathf.Max(0.0001f, e.spawnRate);
        float r = Random.value * total;
        foreach (var e in enemyEntries)
        {
            r -= Mathf.Max(0.0001f, e.spawnRate);
            if (r <= 0f)
            {
                Vector3 pos = new Vector3(Random.Range(areaX.x, areaX.y),
                                          Random.Range(areaY.x, areaY.y), 0f);
                Instantiate(e.prefab, pos, Quaternion.identity);
                return;
            }
        }
    }
}
