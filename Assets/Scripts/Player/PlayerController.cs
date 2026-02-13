using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference boostActionReference;
    public InputActionReference moveActionReference;
    public InputActionReference verticalActionReference;
    public InputActionReference openMenuActionReference;

    [Header("3D / VR Reference")]
    public Transform reference;

    public float verticalSpeed = 2f;

    [Header("Movement")]
    private float baseSpeed;
    public float boostMult = 1.5f;
    public float boostDuration = 2f;
    public float boostCooldown = 5f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;

    [Header("Rotation")]
    public float rotationLerp = 10f;
    public float yawOffset = 0f;

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
    public Damageable damageable;

    public float size;

    private Rigidbody rb;
    private EnergyManager energyManager;
    private bool ledIsOn = false;
    public GameController gc;

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
        gc = FindFirstObjectByType<GameController>();
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;
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
        bool isAlive = damageable != null && !damageable.isDead;

        if (WebSocketController.Instance != null &&
            WebSocketController.Instance.IsConnected)
        {
            if (isAlive && !ledIsOn)
            {
                WebSocketController.Instance.ledIntensity = 255;
                WebSocketController.Instance.SendLedIntensity();
                ledIsOn = true;
            }
            else if (!isAlive && ledIsOn)
            {
                WebSocketController.Instance.ledIntensity = 0;
                WebSocketController.Instance.SendLedIntensity();
                ledIsOn = false;
            }
        }

        if (!isAlive)
        {
            velocity = Vector3.MoveTowards(
                velocity,
                Vector3.zero,
                deceleration * Time.deltaTime);
            return;
        }

        // Inputs
        Vector2 moveInput = moveActionReference != null ? moveActionReference.action.ReadValue<Vector2>() : Vector2.zero;
        float h = moveInput.x;
        float v = moveInput.y;

        bool boostPressed = boostActionReference != null && boostActionReference.action.WasPressedThisFrame();

        float upDown = 0f;
        if (verticalActionReference != null)
            upDown = verticalActionReference.action.ReadValue<float>();

        var statsUpdate = GetComponent<AgentStats>();
        if (statsUpdate) baseSpeed = statsUpdate.CurrentSpeed;

        // Energy / Boost
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
            baseSpeed *= 0.5f;
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

        Transform cam = reference != null ? reference : (Camera.main ? Camera.main.transform : transform);

        // Volle 3D-Blickrichtung der Kamera für Vorwärts/Rückwärts
        Vector3 forward = cam.forward.normalized;

        // Rechts-Vektor horizontal halten für sauberes Strafen
        Vector3 right = cam.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

        Vector3 moveDir = right * h + forward * v;
        moveDir += Vector3.up * upDown * verticalSpeed;

        Vector3 targetVelocity = (moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector3.zero) * currentSpeed;

        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);

        float pushAllowance = boosting ? 3.0f : 1.0f;
        float maxAllowed = currentSpeed + pushAllowance;
        if (velocity.magnitude > maxAllowed) velocity = velocity.normalized * maxAllowed;

        if (damageable != null && damageable.currentHealth > 0f)
        {
            if (rb) rb.MovePosition(rb.position + velocity * Time.deltaTime);

            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f) && Mathf.Approximately(upDown, 0f))
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up)
                    * Quaternion.Euler(0f, yawOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerp * Time.deltaTime);
            }
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (rb) rb.linearVelocity *= 0.6f;
    }

}
