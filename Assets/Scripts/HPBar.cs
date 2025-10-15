using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public Damageable target;          // Root des Fisches
    public Image fill;                 // Vordergrund-Balken (Filled: Horizontal)
    public CanvasGroup group;          // für Ein-/Ausblenden
    public Gradient colorByHp;         // grün→gelb→rot
    public float visibleTime = 1.4f;   // wie lange nach Hit sichtbar
    public float fadeSpeed = 6f;       // Ausblendgeschwindigkeit
    public Vector3 worldOffset = new Vector3(0, 1.0f, 0);

    float tVisible;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (!target) target = GetComponentInParent<Damageable>();
        if (!group) group = GetComponent<CanvasGroup>();
        if (target) target.OnDamaged += OnDamaged;
        group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (target) target.OnDamaged -= OnDamaged;
    }

    void LateUpdate()
    {
        // über dem Fisch „kleben“
        if (target) transform.position = target.transform.position + worldOffset;

        // zur Kamera ausrichten (billboard)
        if (cam) transform.forward = cam.transform.forward;

        // ausblenden nach Zeit
        float targetAlpha = Time.time < tVisible ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    void OnDamaged(float dmg, float normalizedHp)
    {
        if (fill)
        {
            fill.fillAmount = Mathf.Clamp01(normalizedHp);
            if (colorByHp != null) fill.color = colorByHp.Evaluate(fill.fillAmount);
        }
        tVisible = Time.time + visibleTime;
        group.alpha = 1f; // sofort zeigen
    }
}
