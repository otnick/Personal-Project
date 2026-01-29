using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FishAI : MonoBehaviour
{
    [Header("Agent")]
    public AgentStats stats;
    public LayerMask agentMask;       // Layer applied to player and fish
    public float senseRadius = 12f; 
    public float accel = 20f;
    public float idleSpeed = 2f; 
    public float idleTurnEvery = 3f;
    public float sizeMargin = 1.0f;   // larger than = my.size > other.size * sizeMargin
    public Damageable damageable;     // reference to self damageable component

    [Header("Schwimmbereich")]
    public Transform center;          // Ursprung (z. B. Player oder Empty)
    public float swimRadius = 8f;     // Radius der Kugel, in der die Fische bleiben
    public float boundaryStrength = 1.0f; // wie stark sie zurück in die Kugel gelenkt werden

    [Header("Rotation / Look")]
    [Tooltip("Falls dein Mesh nicht nach +Z schaut, trage hier den Euler-Offset ein (z.B. 0, -90, 0).")]
    public Vector3 forwardOffsetEuler = Vector3.zero;
    public float pitchFactor = 4f;    // wie stark pitch auf vertikale Geschwindigkeit reagiert
    public float maxPitch = 35f;      // maximaler Pitch in Grad
    public float rotSpeed = 8f;       // Slerp-Geschwindigkeit

    [Header("Debug")]
    public bool drawDebugGizmos = false;

    Rigidbody rb;
    Vector3 idleDir = Vector3.right;
    float nextIdleTurn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Verhindere Roll (Drehung um lokale Vorwärtsachse). Erlaube Yaw & Pitch durch manuelles Setzen.
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        if (!stats) stats = GetComponent<AgentStats>();
        if (!damageable) damageable = GetComponent<Damageable>();

        if (!center)
        {
            // fallback: ein gemeinsames, simples Global-Center vermeiden mehrere GameObjects pro Fisch
            var go = GameObject.Find("FishCenter_Global");
            if (go == null)
            {
                go = new GameObject("FishCenter_Global");
                go.transform.position = Vector3.zero;
            }
            center = go.transform;
        }

        PickNewIdle();
    }

    void FixedUpdate()
    {
        if (damageable != null && damageable.currentHealth <= 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return; // dead fish don't move
        }

        // Fallback if no agent mask is set
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

        if (nearestBigger)
        {
            // flee
            Vector3 dir = (nearestBigger.position - pos).normalized * -1f;
            MoveSimple(dir, hunt:false);
        }
        else if (nearestSmaller)
        {
            // chase
            Vector3 dir = (nearestSmaller.position - pos).normalized;
            MoveSimple(dir, hunt:true);
        }
        else
        {
            IdleSimple(); // random drift
        }

        // In Kugel halten
        StayInSphere();

        // Dämpfe physikalische Drehung (wir setzen Rotation manuell)
        rb.angularVelocity = Vector3.zero;

        FaceByVelocity();
    }

    void MoveSimple(Vector3 dir, bool hunt)
    {
        float maxSpeed = stats ? stats.CurrentSpeed : 6f;
        if (dir.sqrMagnitude < 1e-6f) return;

        Vector3 desiredVel = dir.normalized * maxSpeed; // no arrive when hunting
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            desiredVel,
            accel * Time.fixedDeltaTime
        );

        // harte Kappe
        var v = rb.linearVelocity;
        if (v.magnitude > maxSpeed)
            rb.linearVelocity = v.normalized * maxSpeed;
    }

    void IdleSimple()
    {
        float maxSpeed = stats ? stats.CurrentSpeed : 6f;
        if (Time.time >= nextIdleTurn) PickNewIdle();

        Vector3 desiredVel = idleDir * Mathf.Min(idleSpeed, maxSpeed * 0.4f);
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            desiredVel,
            (accel * 0.5f) * Time.fixedDeltaTime
        );
    }

    void PickNewIdle()
    {
        // 3D: zufällige Richtung auf der Kugel (gleichmäßig)
        idleDir = Random.onUnitSphere;
        nextIdleTurn = Time.time + idleTurnEvery + Random.Range(-0.8f, 0.8f);
    }

    void StayInSphere()
    {
        if (!center) return;

        Vector3 toCenter = center.position - transform.position;
        float dist = toCenter.magnitude;

        if (dist > swimRadius)
        {
            // Stärke abhängig davon, wie weit außerhalb
            Vector3 dir = toCenter.normalized;
            float maxSpeed = stats ? stats.CurrentSpeed : 6f;

            Vector3 desiredVel = dir * maxSpeed;
            rb.linearVelocity = Vector3.MoveTowards(
                rb.linearVelocity,
                desiredVel,
                accel * boundaryStrength * Time.fixedDeltaTime
            );
        }
    }

    void FaceByVelocity()
    {
        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0004f) return;

        // Yaw (horizontale Ausrichtung) anhand XZ-Komponente
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        Quaternion yawRot;
        if (flat.sqrMagnitude > 1e-6f)
            yawRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
        else
            yawRot = Quaternion.LookRotation(v.normalized, Vector3.up); // fast nur vertikal

        // Pitch: kippen abhängig von vertical speed
        float pitchAngle = Mathf.Clamp(-v.y * pitchFactor, -maxPitch, maxPitch); // negative v.y => downwards tilt
        Quaternion pitchRot = Quaternion.AngleAxis(pitchAngle, Vector3.right);

        Quaternion targetRot = yawRot * pitchRot;

        // Mesh-forward offset (falls das Modell anders orientiert ist)
        if (forwardOffsetEuler != Vector3.zero)
        {
            Quaternion offset = Quaternion.Euler(forwardOffsetEuler);
            targetRot = targetRot * offset;
        }

        // Smooth rotate
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);

        if (drawDebugGizmos)
        {
            Debug.DrawRay(transform.position, v.normalized * 1.2f, Color.green, 0.1f);
            Debug.DrawLine(transform.position, transform.position + transform.forward * 1.2f, Color.yellow, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!center) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, swimRadius);
    }
}
