using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference boostActionReference;
    public InputActionReference moveActionReference;

    [Tooltip("Optional: -1..1 (z.B. Right Stick Y). Für Trigger-Setup siehe Kommentar unten.")]
    public InputActionReference verticalActionReference;

    [Header("3D / VR Reference")]
    [Tooltip("Referenz für die Bewegungsrichtung (meist XR Camera / Head). Wenn leer: Camera.main.")]
    public Transform reference;

    [Tooltip("Wenn true: Stick steuert nur XZ (horizontal). Hoch/Runter über verticalActionReference.")]
    public bool planar = true;

    [Tooltip("Wie stark Hoch/Runter wirkt (nur relevant, wenn planar=true und verticalActionReference belegt).")]
    public float verticalSpeed = 2f;

    [Header("Movement")]
    private float baseSpeed;            // kommt aus AgentStats
    private float size;
    public float boostMult = 1.5f;      // 1.5x schneller beim Boost
    public float boostDuration = 2f;
    public float boostCooldown = 5f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;

    [Header("Rotation")]
    [Tooltip("Wie schnell der Fisch in Bewegungsrichtung dreht.")]
    public float rotationLerp = 10f;

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
    private EnergyManager energyManager;

    // fade in
    public System.Collections.IEnumerator FadeIn()
    {
        float t = 0f, dur = Mathf.Max(0.01f, fadeInDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            if (fadeGroup) fadeGroup.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        if (fadeGroup) fadeGroup.alpha = 0f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;

            // 3D: Position NICHT einfrieren (anders als 2D Top-Down)
            // Standard: Roll verhindern (Z-Rotation), ggf. auch Pitch (X) verhindern
            rb.constraints = RigidbodyConstraints.FreezeRotationZ;

            rb.linearDamping = 2f;
            rb.angularDamping = 999f;
        }

        var stats = GetComponent<AgentStats>();
        damageable = GetComponent<Damageable>();
        energyManager = GetComponent<EnergyManager>();

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

        if (fadeGroup)
        {
            fadeGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        if (damageable != null && damageable.isDead)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
            return; // no movement or actions when dead
        }

        // ---- Inputs ----
        Vector2 moveInput = moveActionReference != null ? moveActionReference.action.ReadValue<Vector2>() : Vector2.zero;
        float h = moveInput.x;
        float v = moveInput.y;

        bool boostPressed = boostActionReference != null && boostActionReference.action.WasPressedThisFrame();

        // Optional vertical input (-1..1 empfohlen)
        float upDown = 0f;
        if (verticalActionReference != null)
            upDown = verticalActionReference.action.ReadValue<float>();

        // Basis-Speed evtl. aus Stats aktualisieren
        var stats = GetComponent<AgentStats>();
        if (stats) baseSpeed = stats.CurrentSpeed;

        // ---- Energy / Boost ----
        if (energyManager != null && energyManager.currentEnergy > 0f)
        {
            if (boosting)
            {
                energyManager.currentEnergy = Mathf.Max(
                    0f,
                    energyManager.currentEnergy - energyManager.energyDrainageRate * Time.deltaTime * 2f
                );

                if (energyManager.currentEnergy <= 0f) boosting = false;
            }
        }
        else
        {
            boosting = false;
            baseSpeed = baseSpeed * 0.5f;
        }

        if (boostPressed &&
            Time.time >= nextBoostTime &&
            !boosting &&
            damageable != null &&
            damageable.currentHealth > 0f &&
            energyManager != null &&
            energyManager.currentEnergy > 0f)
        {
            boosting = true;
            boostEndTime = Time.time + boostDuration;
            nextBoostTime = boostEndTime + boostCooldown;
        }

        if (boosting && Time.time >= boostEndTime) boosting = false;

        float currentSpeed = baseSpeed * (boosting ? boostMult : 1f);

        // ---- 3D Movement (kamera-relativ) ----
        Transform cam = reference != null ? reference : (Camera.main ? Camera.main.transform : transform);

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        if (planar)
        {
            // nur horizontal (XZ)
            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        }

        Vector3 moveDir = (right * h + forward * v);

        if (planar)
        {
            // Hoch/Runter getrennt
            moveDir += Vector3.up * upDown * verticalSpeed;
        }
        else
        {
            // Wenn du wirklich 3D relativ zur Blickrichtung willst, könntest du zusätzlich Pitch erlauben.
            // Standardmäßig lassen wir es trotzdem wie oben.
            moveDir += Vector3.up * upDown * verticalSpeed;
        }

        Vector3 targetVelocity = (moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector3.zero) * currentSpeed;

        // Beschleunigen
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);

        // kleines Fremdschub-Polster
        float pushAllowance = boosting ? 3.0f : 1.0f;
        float maxAllowed = currentSpeed + pushAllowance;
        if (velocity.magnitude > maxAllowed) velocity = velocity.normalized * maxAllowed;

        // ---- Apply Movement ----
        if (damageable != null && damageable.currentHealth > 0f)
        {
            if (rb) rb.MovePosition(rb.position + velocity * Time.deltaTime);

            // Bremsen bei keinem Input
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f) && Mathf.Approximately(upDown, 0f))
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

            // ---- Rotation: in Bewegungsrichtung schauen ----
            Vector3 velFlat = velocity;

            // Wenn du bei planar=true keinen Pitch willst, kannst du Y für Rotation ignorieren:
            // velFlat.y = 0f;

            if (velFlat.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velFlat.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerp * Time.deltaTime);
            }
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (rb) rb.linearVelocity *= 0.6f; // Stöße dämpfen
    }
}

/*
Trigger-Setup für Up/Down (typisch in VR):
- Lege zwei InputActions an:
  LeftTrigger (0..1) und RightTrigger (0..1)
- Dann:
  upDown = rightTriggerValue - leftTriggerValue;  // ergibt -1..1
*/
