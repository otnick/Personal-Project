using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    public EnergyManager target;
    public Image fill;
    public TMPro.TMP_Text energyText;
    public Gradient colorByEnergy;
    public float fadeSpeed = 6f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (!target) target = GetComponentInParent<EnergyManager>();
        if (target) target.OnEnergyChanged += OnEnergyChanged;

        if (group) group.alpha = 1f; // immer sichtbar
    }

    void OnDestroy()
    {
        if (target) target.OnEnergyChanged -= OnEnergyChanged;
    }

    void OnEnergyChanged(float delta, float normalized)
    {
        fill.fillAmount = Mathf.Clamp01(normalized);
        fill.color = colorByEnergy.Evaluate(fill.fillAmount);
        if (energyText)
            energyText.text = $"{Mathf.RoundToInt(target.currentEnergy)} / {Mathf.RoundToInt(target.maxEnergy)}";
    }
}