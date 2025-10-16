using UnityEngine;

public class AgentStats : MonoBehaviour
{
    [Header("Core")]
    public float size = 1f;        // relative size
    public float baseSpeed = 6f;     // base speed
    public Transform visual;         // scales with size

    [Header("Tuning")]
    public float speedPerSize = 0.5f; // how much size affects speed

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
