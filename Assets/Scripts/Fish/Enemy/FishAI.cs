using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FishAI : MonoBehaviour
{
    [Header("Agent")]
    public AgentStats stats;
    [Tooltip("IMPORTANT: should include the Fish layer(s). If you include Player here, fish may chase player depending on size.")]
    public LayerMask agentMask;
    public float senseRadius = 12f;
    public float accel = 20f;
    public float idleSpeed = 2f;
    public float idleTurnEvery = 3f;
    [Tooltip("larger than = my.size > other.size * sizeMargin")]
    public float sizeMargin = 1.0f;
    public Damageable damageable;

    [Header("Schwimmbereich")]
    public Transform center;              
    public float swimRadius = 8f;
    public float boundaryStrength = 1.0f;

    [Header("Rotation / Look")]
    [Tooltip("Falls dein Mesh nicht nach +Z schaut, trage hier den Euler-Offset ein (z.B. 0, -90, 0).")]
    public Vector3 forwardOffsetEuler = Vector3.zero;
    public float pitchFactor = 4f;
    public float maxPitch = 35f;
    public float rotSpeed = 8f;

    [Header("Attack")]
    [Tooltip("Distance at which a bite is applied (root-to-root distance).")]
    public float attackRange = 0.8f;
    public float biteDamage = 10f;
    public float biteCooldown = 0.6f;

    [Header("Grab / XR (set by a bridge script)")]
    public bool isGrabbed;

    [Header("Debug")]
    public bool drawDebugGizmos = false;
    public bool debugDrawTarget = false;

    Rigidbody rb;
    Vector3 idleDir = Vector3.right;
    float nextIdleTurn;
    float nextBiteTime = 0f;

    Transform currentTargetSmaller;
    Transform currentThreatBigger;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Prevent roll
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        // VR-stable defaults
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (!stats) stats = GetComponent<AgentStats>();
        if (!damageable) damageable = GetComponent<Damageable>();

        if (!center)
        {
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
        // XR grab pauses AI
        if (isGrabbed)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // Dead fish don't move
        if (damageable != null && damageable.currentHealth <= 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Fallback if no mask is set
        int mask = agentMask.value != 0 ? agentMask.value : ~0;

        // Detect nearby agents
        var hits = Physics.OverlapSphere(
            transform.position,
            senseRadius,
            mask,
            QueryTriggerInteraction.Collide
        );

        Transform nearestSmaller = null; float dSmall = float.MaxValue;
        Transform nearestBigger = null;  float dBig = float.MaxValue;

        float my = stats ? stats.size : 1f;
        Vector3 pos = transform.position;

        // IMPORTANT:
        // Colliders are on child objects ("Collider"), AgentStats is on root.
        // So we must resolve to the parent/root via GetComponentInParent.
        foreach (var h in hits)
        {
            var otherStats = h.GetComponentInParent<AgentStats>();
            if (!otherStats) continue;

            Transform otherRoot = otherStats.transform;
            if (otherRoot == transform) continue;

            float other = otherStats.size;
            float distSqr = (otherRoot.position - pos).sqrMagnitude;

            if (my > other * sizeMargin)
            {
                if (distSqr < dSmall) { dSmall = distSqr; nearestSmaller = otherRoot; }
            }
            else if (other > my * sizeMargin)
            {
                if (distSqr < dBig) { dBig = distSqr; nearestBigger = otherRoot; }
            }
        }

        currentTargetSmaller = nearestSmaller;
        currentThreatBigger = nearestBigger;

        // Behaviour
        if (nearestBigger)
        {
            // flee
            Vector3 dir = (nearestBigger.position - pos).normalized * -1f;
            MoveSimple(dir);
        }
        else if (nearestSmaller)
        {
            Vector3 toTarget = nearestSmaller.position - pos;

            // chase
            if (toTarget.sqrMagnitude > 1e-6f)
                MoveSimple(toTarget.normalized);

            // bite when close (root-to-root distance)
            float dist = toTarget.magnitude;
            if (dist <= attackRange && Time.time >= nextBiteTime)
            {
                TryBite(nearestSmaller);
                nextBiteTime = Time.time + biteCooldown;
            }
        }
        else
        {
            // random drift
            IdleSimple();
        }

        // Keep in swim sphere
        StayInSphere();

        // We set rotation manually
        rb.angularVelocity = Vector3.zero;
        FaceByVelocity();

        if (debugDrawTarget)
        {
            if (nearestSmaller) Debug.DrawLine(pos, nearestSmaller.position, Color.red, 0.02f);
            if (nearestBigger) Debug.DrawLine(pos, nearestBigger.position, Color.blue, 0.02f);
        }
    }

    void TryBite(Transform targetRoot)
    {
        if (targetRoot == null) return;

        // Ensure it is still smaller (in case sizes changed)
        float my = stats ? stats.size : 1f;
        var otherStats = targetRoot.GetComponent<AgentStats>();
        float other = otherStats ? otherStats.size : 1f;
        if (!(my > other * sizeMargin)) return;

        var otherDamageable = targetRoot.GetComponent<Damageable>();
        if (!otherDamageable) return;

        // If your Damageable has a method, prefer it:
        // otherDamageable.TakeDamage(biteDamage);

        // Fallback (matches your earlier usage of currentHealth):
        otherDamageable.currentHealth -= biteDamage;
        if (otherDamageable.currentHealth < 0f) otherDamageable.currentHealth = 0f;
    }

    void MoveSimple(Vector3 dir)
    {
        float maxSpeed = stats ? stats.CurrentSpeed : 6f;
        if (dir.sqrMagnitude < 1e-6f) return;

        Vector3 desiredVel = dir.normalized * maxSpeed;
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            desiredVel,
            accel * Time.fixedDeltaTime
        );

        // hard cap
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

        // Yaw from XZ
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        Quaternion yawRot;
        if (flat.sqrMagnitude > 1e-6f)
            yawRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
        else
            yawRot = Quaternion.LookRotation(v.normalized, Vector3.up);

        // Pitch from vertical speed
        float pitchAngle = Mathf.Clamp(-v.y * pitchFactor, -maxPitch, maxPitch);
        Quaternion pitchRot = Quaternion.AngleAxis(pitchAngle, Vector3.right);

        Quaternion targetRot = yawRot * pitchRot;

        // Mesh forward offset
        if (forwardOffsetEuler != Vector3.zero)
        {
            Quaternion offset = Quaternion.Euler(forwardOffsetEuler);
            targetRot = targetRot * offset;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);

        if (drawDebugGizmos)
        {
            Debug.DrawRay(transform.position, v.normalized * 1.2f, Color.green, 0.1f);
            Debug.DrawLine(transform.position, transform.position + transform.forward * 1.2f, Color.yellow, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (center)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center.position, swimRadius);
        }

        if (debugDrawTarget)
        {
            Gizmos.color = Color.red;
            if (currentTargetSmaller) Gizmos.DrawLine(transform.position, currentTargetSmaller.position);
            Gizmos.color = Color.blue;
            if (currentThreatBigger) Gizmos.DrawLine(transform.position, currentThreatBigger.position);
        }
    }
}