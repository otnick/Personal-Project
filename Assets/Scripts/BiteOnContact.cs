using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BiteOnContact : MonoBehaviour
{
    public float damage = 25f;
    public float biteCooldown = 0.4f;
    public LayerMask agentMask;          // z. B. "Agent"
    public AgentStats attackerStats;     // vom Parent
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

        // Layer-Fallback: wenn nicht gesetzt, Everything
        int mask = agentMask.value != 0 ? agentMask.value : ~0;
        if (((1 << other.gameObject.layer) & mask) == 0) return;

        // Nicht sich selbst
        if (other.transform.root == transform.root) return;

        // Komponenten am Root/Parent suchen
        var targetStats = other.GetComponentInParent<AgentStats>();
        var targetHp    = other.GetComponentInParent<Damageable>();
        if (targetStats == null || targetHp == null) return;

        //----------------------------------------------------------
        // Immer beißen, aber Schaden skaliert mit Größenverhältnis
        //----------------------------------------------------------
        float ratio = Mathf.Max(0.001f, attackerStats.size / targetStats.size);
        float dmgMult = Mathf.Clamp(ratio, 0.2f, 2.5f);  // kleiner → 20 %, viel größer → 250 %
        float finalDamage = damage * dmgMult;

        var attackerHp = GetComponentInParent<Damageable>();
        if (attackerHp != null && attackerHp.currentHealth > 0f)
        {
            attackerHp.Heal(finalDamage * 0.5f);  // heile z. B. 50 % des angerichteten Schadens
        }

        // Optional: etwas Wachstum oder Animation
        if (animatorScript) animatorScript.PlayEatAnimation();
        if (biteSound) biteSound.Play();
        attackerStats.Grow(0.02f * ratio);

        // Option: leichter Rückstoß, wenn deutlich kleiner
        if (ratio < 1f)
        {
            var rb = attackerStats.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity *= 0.7f;
        }

        nextBiteTime = Time.time + biteCooldown;
        // Debug.Log($"{name} bit {other.name} for {finalDamage}");
    }
}
