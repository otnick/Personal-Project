using UnityEngine;
using System;

public class EnergyManager : MonoBehaviour
{
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyDrainageRate = 2f;

    void Awake() => currentEnergy = maxEnergy;

    void Start()
    {
        InvokeRepeating(nameof(DrainEnergy), 1f, 1f);
    }

    void DrainEnergy()
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - energyDrainageRate);
    }
    public void ReplenishEnergy(float amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }
}
