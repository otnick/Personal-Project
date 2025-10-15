using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FishAI : MonoBehaviour
{
    public AgentStats stats;
    public LayerMask agentMask;       // Layer für Spieler+Gegner
    public float senseRadius = 12f;   // Wahrnehmungsradius
    public float accel = 20f;         // wie schnell wir Richtung Zielgeschwindigkeit blenden
    public float idleSpeed = 2f;      // Basisspeed im Random-Drift
    public float idleTurnEvery = 3f;  // Sekunden bis neue Idle-Richtung
    public float sizeMargin = 1.0f;   // "größer als" = my.size > other.size * sizeMargin
    public Damageable damageable; // reference to self damageable component

    Rigidbody rb;
    Vector3 idleDir = Vector3.right;
    float nextIdleTurn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        if (!stats) stats = GetComponent<AgentStats>();
        if (!damageable) damageable = GetComponent<Damageable>();
        PickNewIdle();
    }

    void FixedUpdate()
    {
        // Fallback
        int mask = agentMask.value != 0 ? agentMask.value : ~0;

        var hits = Physics.OverlapSphere(
            transform.position,
            senseRadius,
            mask,
            QueryTriggerInteraction.Collide
        );

        Transform nearestSmaller = null; float dSmall = float.MaxValue;
        Transform nearestBigger  = null; float dBig   = float.MaxValue;

        float my = stats ? stats.size : 1f;
        Vector3 pos = transform.position;

        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            var s = h.GetComponent<AgentStats>(); if (!s) continue;

            float other = s.size;
            float distSqr = (h.transform.position - pos).sqrMagnitude;

            if (my > other * sizeMargin)
            {
                if (distSqr < dSmall) { dSmall = distSqr; nearestSmaller = h.transform; }
            }
            else if (other > my * sizeMargin)
            {
                if (distSqr < dBig) { dBig = distSqr; nearestBigger = h.transform; }
            }
        }
        if (nearestBigger ) MoveSimple((nearestBigger.position - pos).normalized * -1f, hunt:false); // flee
        else if (nearestSmaller) MoveSimple((nearestSmaller.position - pos).normalized, hunt:true);  // chase
        else IdleSimple(); // random drift

        FaceByVelocity();
    }

    void MoveSimple(Vector3 dir, bool hunt)
    {
        if(damageable != null && damageable.currentHealth <= 0f) return; // dead fish don't move
        float maxSpeed = stats ? stats.CurrentSpeed : 6f;
        dir.z = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        Vector3 desiredVel = dir.normalized * maxSpeed; // beim Jagen KEIN Arrive – Kontakt erwünscht
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            desiredVel,
            accel * Time.fixedDeltaTime
        );

        // harte Kappe
        var v = rb.linearVelocity; v.z = 0f;
        if (v.magnitude > maxSpeed) rb.linearVelocity = v.normalized * maxSpeed;
    }

    void IdleSimple()
    {
        if(damageable != null && damageable.currentHealth <= 0f) return; // dead fish don't move
        if (Time.time >= nextIdleTurn) PickNewIdle();
        float maxSpeed = stats ? stats.CurrentSpeed : 6f;

        Vector3 desiredVel = idleDir * Mathf.Min(idleSpeed, maxSpeed * 0.4f);
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            new Vector3(desiredVel.x, desiredVel.y, 0f),
            (accel * 0.5f) * Time.fixedDeltaTime
        );
    }

    void PickNewIdle()
    {
        var r = Random.insideUnitCircle.normalized;
        idleDir = new Vector3(r.x, r.y, 0f);
        nextIdleTurn = Time.time + idleTurnEvery + Random.Range(-0.8f, 0.8f);
    }

    void FaceByVelocity()
    {
        Vector3 v = rb.linearVelocity; v.z = 0f;
        if (v.sqrMagnitude < 0.0004f) return;

        float yFlip = v.x >= 0f ? 0f : 180f;
        float zTilt = Mathf.Clamp(v.y * 10f, -25f, 25f);
        transform.rotation = Quaternion.Euler(0f, yFlip, zTilt);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, senseRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, senseRadius);
    }
#endif
}
