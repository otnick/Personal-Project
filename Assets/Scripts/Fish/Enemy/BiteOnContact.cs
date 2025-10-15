using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BiteOnContact : MonoBehaviour
{
    public float damage = 25f;
    public float biteCooldown = 0.4f;
    public LayerMask agentMask;
    public AgentStats attackerStats;
    public AnimatorScript animatorScript;
    public AudioSource biteSound;

    float nextBiteTime;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!attackerStats) attackerStats = GetComponentInParent<AgentStats>();
        if (!animatorScript) animatorScript = GetComponentInParent<AnimatorScript>();
    }

    void OnTriggerStay(Collider other)
    {
        if (Time.time < nextBiteTime) return;
        if (!attackerStats) return;

        // Layerfallback
        int mask = agentMask.value != 0 ? agentMask.value : ~0;
        if (((1 << other.gameObject.layer) & mask) == 0) return;

        // Nicht sich selbst
        if (other.transform.root == transform.root) return;

        // Enemy has to have stats + health
        var targetStats = other.GetComponentInParent<AgentStats>();
        var targetHp    = other.GetComponentInParent<Damageable>();
        if (targetStats == null || targetHp == null || targetHp.currentHealth <= 0f) return;

        // Always bite
        var attackerHp = GetComponentInParent<Damageable>();
        if (attackerHp != null && attackerHp.currentHealth > 0f)
        {
            float ratio = Mathf.Max(0.001f, attackerStats.size / targetStats.size);
            float dmgMult = Mathf.Clamp(ratio, 0.2f, 2.5f);  // smaller → 20 %, much larger → 250 %
            float finalDamage = damage * dmgMult;
            targetHp.TakeDamage(finalDamage);

            // maybe not cool for now
            // attackerHp.Heal(finalDamage * 0.5f);  // heal % of dealt damage


            //  sound/animation
            if (animatorScript) animatorScript.PlayEatAnimation();
            if (biteSound) biteSound.Play();
            attackerStats.Grow(0.02f * ratio);

            // knockback
            if (ratio < 1f)
            {
                var rb = attackerStats.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity *= 0.7f;
            }

            nextBiteTime = Time.time + biteCooldown;
        }
    }
}
