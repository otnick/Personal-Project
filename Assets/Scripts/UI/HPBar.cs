using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HpBar : MonoBehaviour
{
    public Damageable target;          // Root des Fisches
    public Image fill;                 // Vordergrund-Balken (Filled: Horizontal)
    public CanvasGroup group;          // für Ein-/Ausblenden
    public Gradient colorByHp;         // grün→gelb→rot
    public float visibleTime = 1.4f;   // wie lange nach Hit sichtbar
    public float fadeSpeed = 6f;       // Ausblendgeschwindigkeit
    public Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    float tVisible;
    Camera cam;
    RectTransform rt;
    Canvas rootCanvas;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        cam = Camera.main;

        if (!target) target = GetComponentInParent<Damageable>();
        if (!group)  group  = GetComponent<CanvasGroup>();

        if (target) target.OnDamaged += OnDamaged;
        if (group)  group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (target) target.OnDamaged -= OnDamaged;
    }

    void LateUpdate()
    {
        if (!target || rt == null || rootCanvas == null) return;

        Vector3 worldPos = target.transform.position + worldOffset;

        switch (rootCanvas.renderMode)
        {
            case RenderMode.WorldSpace:
                // Direkt in die Welt setzen + „billboarden“
                transform.position = worldPos;
                if (cam) transform.forward = cam.transform.forward;
                break;

            case RenderMode.ScreenSpaceCamera:
            {
                if (!cam) cam = rootCanvas.worldCamera ?? Camera.main;
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                RectTransform canvasRT = rootCanvas.transform as RectTransform;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, cam, out local);
                rt.anchoredPosition = local;
                break;
            }

            case RenderMode.ScreenSpaceOverlay:
            default:
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos); // cam darf null sein bei Overlay
                RectTransform canvasRT = rootCanvas.transform as RectTransform;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, null, out local);
                rt.anchoredPosition = local;
                break;
            }
        }

        // Ein-/Ausblenden
        if (group)
        {
            float targetAlpha = Time.time < tVisible ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    void OnDamaged(float dmg, float normalizedHp)
    {
        if (fill)
        {
            fill.fillAmount = Mathf.Clamp01(normalizedHp);
            if (colorByHp != null) fill.color = colorByHp.Evaluate(fill.fillAmount);
        }
        tVisible = Time.time + visibleTime;
        if (group) group.alpha = 1f; // sofort zeigen
    }
}
