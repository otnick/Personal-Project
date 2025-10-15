using UnityEngine;
using System;

public class Damageable : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    public event Action<float, float> OnDamaged; // (damage, normalizedHp 0..1)

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnDamaged?.Invoke(dmg, currentHealth / Mathf.Max(0.0001f, maxHealth));
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnDamaged?.Invoke(-amount, currentHealth / Mathf.Max(0.0001f, maxHealth));
    }

    void Die() => Destroy(gameObject);
}
