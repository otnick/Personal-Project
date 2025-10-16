using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    private float baseSpeed;        // kommt aus AgentStats
    private float size;
    public float boostMult = 1.5f;  // 1.5x schneller beim Boost
    public float boostDuration = 2f;
    public float boostCooldown = 5f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;

    [Header("Visual Tilt/Flip")]
    public float tiltMaxDeg = 40f;
    public float tiltLerp = 36f;

    [Header("Refs")]
    public AnimatorScript animatorScript;
    public AudioSource biteSound;
    public CanvasGroup fadeGroup;

    [Header("Fade")]
    public float fadeInDuration = 3f;

    [Header("Boost")]
    public bool boosting = false;
    private float boostEndTime = 0f;
    private float nextBoostTime = 0f;

    [Header("Bite Attack")]
    public float damage = 10f;
    public float biteCooldown = 0.4f;
    public LayerMask agentMask;

    [Header("Stats & Damage")]
    public AgentStats attackerStats;
    public Damageable damageable; // reference to self damageable component
    private Rigidbody rb;

    // fade in
    public System.Collections.IEnumerator FadeIn()
    {
        float t = 0f, dur = Mathf.Max(0.01f, fadeInDuration);
        while (t < dur) { t += Time.deltaTime; fadeGroup.alpha = 1f - Mathf.Clamp01(t / dur); yield return null; }
        fadeGroup.alpha = 0f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 2f;
            rb.angularDamping = 999f;
        }

        var stats = GetComponent<AgentStats>();
        damageable = GetComponent<Damageable>();
        if (stats)
        {
            baseSpeed = stats.CurrentSpeed;
            size = stats.size;
        }
        else
        {
            baseSpeed = 2f;
            size = 5f;
        }

        if (fadeGroup) { fadeGroup.alpha = 1f; StartCoroutine(FadeIn()); }
    }

    void Update()
    {
        if(damageable != null && damageable.isDead)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
            return; // no movement or actions when dead
        }
        // inputs
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool shiftDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        // Basis-Speed evtl. aus Stats aktualisieren
        var stats = GetComponent<AgentStats>();
        if (stats) baseSpeed = stats.CurrentSpeed;

        // Boost starten? (einmalig, mit Cooldown)
        if (shiftDown && Time.time >= nextBoostTime && !boosting && damageable != null && damageable.currentHealth > 0f)
        {
            boosting = true;
            boostEndTime = Time.time + boostDuration;
            nextBoostTime = boostEndTime + boostCooldown;
        }
        // Boost beenden, wenn Zeit rum
        if (boosting && Time.time >= boostEndTime) boosting = false;

        float currentSpeed = baseSpeed * (boosting ? boostMult : 1f);

        // Bewegung
        Vector3 targetVelocity = new Vector3(h, v, 0f).normalized * currentSpeed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);

        // kleines Fremdschub-Polster
        float pushAllowance = boosting ? 3.0f : 1.0f;
        float maxAllowed = currentSpeed + pushAllowance;
        if (velocity.magnitude > maxAllowed) velocity = velocity.normalized * maxAllowed;

        // check for death before applying movement
        if (damageable != null && damageable.currentHealth > 0f)
        {
            if (rb) rb.MovePosition(rb.position + velocity * Time.deltaTime);
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

            // Rotation (Flip+Tilt)
            float targetTiltZ = v * tiltMaxDeg;
            float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);
            float targetY = transform.eulerAngles.y;
            if (h > 0f) targetY = 0f; else if (h < 0f) targetY = 180f;
            transform.rotation = Quaternion.Euler(0f, targetY, currentTiltZ);
        }
    }

    void OnCollisionEnter(Collision c)
    {
        rb.linearVelocity *= 0.6f; // Stöße dämpfen
    }
}
