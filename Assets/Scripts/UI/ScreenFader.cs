using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public CanvasGroup group;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (group) group.alpha = 0f;
        DontDestroyOnLoad(gameObject);
    }

    public System.Collections.IEnumerator FadeOut(float duration)
    {
        if (!group) yield break;
        float t = 0f, inv = 1f / Mathf.Max(0.0001f, duration);
        while (group.alpha < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            group.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        group.alpha = 1f;
    }

    public System.Collections.IEnumerator FadeIn(float duration)
    {
        if (!group) yield break;
        float t = 1f, inv = 1f / Mathf.Max(0.0001f, duration);
        while (group.alpha > 0f)
        {
            t -= Time.unscaledDeltaTime * inv;
            group.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        group.alpha = 0f;
    }
}
