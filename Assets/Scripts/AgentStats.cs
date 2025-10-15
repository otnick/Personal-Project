using UnityEngine;

public class AgentStats : MonoBehaviour
{
    [Header("Core")]
    public float size = 1f;        // relativer Größenwert
    public float baseSpeed = 6f;     // Grundgeschwindigkeit
    public Transform visual;         // optional: skaliert mit size

    [Header("Tuning")]
    public float speedPerSize = 0.5f; // wie stark Größe die Speed beeinflusst

    void OnValidate() { ApplySizeToVisual(); }
    void Start()       { ApplySizeToVisual(); }

    public float CurrentSpeed => baseSpeed * (1f + (size - 1f) * speedPerSize);

    public void Grow(float amount)
    {
        size = Mathf.Max(0.1f, size + amount);
        ApplySizeToVisual();
    }

    void ApplySizeToVisual()
    {
        if (visual != null)
            visual.localScale = Vector3.one * size;
    }
}
