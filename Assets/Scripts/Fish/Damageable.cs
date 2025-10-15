using UnityEngine;
using System;

public class Damageable : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;
    private Animator animator;

    public event Action<float, float> OnDamaged; // (damage, normalizedHp 0..1)

    void Awake() => currentHealth = maxHealth;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        InvokeRepeating(nameof(RestoreHealth), 1f, 1f);
    }

    public bool TakeDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnDamaged?.Invoke(dmg, currentHealth / Mathf.Max(0.0001f, maxHealth));
        if (currentHealth <= 0f) Die();
        return currentHealth <= 0f;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnDamaged?.Invoke(-amount, currentHealth / Mathf.Max(0.0001f, maxHealth));
    }

    public void RestoreHealth()
    {
        if (currentHealth < maxHealth && currentHealth > 0f)
        {
            Heal(0.5f);
        }
    }

    void Die()
    {
        animator?.SetBool("dead", true);
        animator?.SetTrigger("die");
        Destroy(gameObject, 3f);
    }
}
