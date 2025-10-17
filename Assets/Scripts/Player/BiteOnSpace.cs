using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BiteOnSpace : MonoBehaviour
{
    [Header("Bite")]
    public float damage = 25f;
    public float biteCooldown = 0.4f;
    public LayerMask agentMask;
    public float fallbackRadius = 0.8f; 

    [Header("Refs")]
    public AgentStats attackerStats;
    public AnimatorScript animatorScript;
    public AudioSource biteSound;
    public GameController gameController;
    private EnergyManager energyManager;

    float nextBiteTime;

    void Start()
    {
        energyManager = GetComponentInParent<EnergyManager>();
        gameController = FindFirstObjectByType<GameController>();
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!attackerStats)  attackerStats  = GetComponentInParent<AgentStats>();
        if (!animatorScript) animatorScript = GetComponentInParent<AnimatorScript>();
        if (!biteSound)      biteSound      = GetComponentInParent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TryBite();
    }

    void TryBite()
    {
        if (Time.time < nextBiteTime) return;
        if (!attackerStats) return;

        // attacker alive?
        var attackerHp = GetComponentInParent<Damageable>();
        if (attackerHp == null || attackerHp.currentHealth <= 0f) return;

        // calc range
        float radius = GetBiteRadius();

        // check for overlapping agents
        int mask = agentMask.value != 0 ? agentMask.value : ~0;
        var hits = Physics.OverlapSphere(
            transform.position, radius, mask, QueryTriggerInteraction.Collide
        );

        // Find next valid target (with Damageable + AgentStats, not self)
        Damageable targetHp = null;
        AgentStats targetStats = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (h.transform.root == transform.root) continue;

            var hp   = h.GetComponentInParent<Damageable>();
            var stat = h.GetComponentInParent<AgentStats>();
            if (hp == null || stat == null || hp.currentHealth <= 0f) continue;

            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                targetHp = hp;
                targetStats = stat;
            }
        }

        if (animatorScript) animatorScript.PlayEatAnimation();
        if (biteSound) biteSound.Play();

        if (targetHp != null && targetStats != null)
        {
            // damage + possible heal
            float ratio   = Mathf.Max(0.001f, attackerStats.size / targetStats.size);
            float dmgMult = Mathf.Clamp(ratio, 0.2f, 2.5f); // smaller → 20 %, much larger → 250 %
            float finalDamage = damage * dmgMult;

            if (energyManager != null)
            {
                // Energiegewinn beim erfolgreichen Biss
                energyManager.currentEnergy = Mathf.Min(energyManager.maxEnergy, energyManager.currentEnergy + finalDamage * 0.5f);
            }

            bool isDead = targetHp.TakeDamage(finalDamage);
            if (isDead && gameController != null)
            {
                gameController.killCounter.AddKill();
            }

            // maybe not cool for now
            // attackerHp.Heal(finalDamage * 0.5f);

            attackerStats.Grow(0.02f * ratio);

            if (ratio < 1f)
            {
                var rb = attackerStats.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity *= 0.7f;
            }
        }

        nextBiteTime = Time.time + biteCooldown;
    }

    float GetBiteRadius()
    {
        // range from bite collider
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        var sphere = GetComponent<SphereCollider>();
        if (sphere) return sphere.radius * scale;

        var capsule = GetComponent<CapsuleCollider>();
        if (capsule)
        {
            float r = capsule.radius * scale;
            float h = (capsule.height * 0.5f) * scale;
            return Mathf.Max(r, h * 0.5f);
        }

        var box = GetComponent<BoxCollider>();
        if (box)
        {
            Vector3 ext = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
            return ext.magnitude;
        }

        return fallbackRadius;
    }
}