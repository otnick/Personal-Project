using UnityEngine;
using System;

public class EnergyManager : MonoBehaviour
{
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyDrainageRate = 2f;

    // Event: (delta, normalized 0..1)
    public event Action<float, float> OnEnergyChanged;

    void Awake()
    {
        currentEnergy = maxEnergy;
        RaiseEnergyChanged(0f);
    }

    void Start()
    {
        InvokeRepeating(nameof(DrainEnergy), 1f, 1f);
    }

    void DrainEnergy()
    {
        float before = currentEnergy;
        currentEnergy = Mathf.Max(0f, currentEnergy - energyDrainageRate);
        RaiseEnergyChanged(currentEnergy - before);
    }

    public void ReplenishEnergy(float amount)
    {
        float before = currentEnergy;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        RaiseEnergyChanged(currentEnergy - before);
    }

    void RaiseEnergyChanged(float delta)
    {
        float norm = maxEnergy > 0f ? (currentEnergy / maxEnergy) : 0f;
        OnEnergyChanged?.Invoke(delta, norm);
    }
}
