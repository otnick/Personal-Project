using UnityEngine;
using TMPro;

public class HitsplatSpawner : MonoBehaviour
{
    public Damageable target;
    public GameObject hitsplatPrefab;   // TMP Text World-Space
    public Vector3 spawnOffset = new Vector3(0, 1.1f, 0);

    void Awake()
    {
        if (!target) target = GetComponentInParent<Damageable>();
        if (target) target.OnDamaged += OnDamaged;
    }
    void OnDestroy()
    {
        if (target) target.OnDamaged -= OnDamaged;
    }

    void OnDamaged(float dmg, float normalizedHp)
    {
        if (!hitsplatPrefab) return;
        var go = Instantiate(hitsplatPrefab, target.transform.position + spawnOffset, Quaternion.identity);
        var t  = go.GetComponentInChildren<TextMeshProUGUI>();
        if (t)
        {
            bool heal = dmg < 0f;
            t.text = heal ? Mathf.RoundToInt(-dmg).ToString() : Mathf.RoundToInt(dmg).ToString();
            t.color = heal ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.35f, 0.35f);
        }
        go.AddComponent<HitsplatFloat>();
    }
}

public class HitsplatFloat : MonoBehaviour
{
    float life = 0.8f;
    float speed = 0.8f;
    CanvasGroup group;

    void Awake()
    {
        group = gameObject.GetComponent<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        life -= Time.deltaTime;
        group.alpha = Mathf.Clamp01(life * 1.25f);
        if (life <= 0f) Destroy(gameObject);
    }
}
