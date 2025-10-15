using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    private float speed;
    private float size;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;

    [Header("Visual Tilt/Flip")]
    public float tiltMaxDeg = 40f;
    public float tiltLerp = 36f;

    [Header("Refs")]
    public AnimatorScript animatorScript;
    public AudioSource audioSource;
    public CanvasGroup fadeGroup;

    [Header("Fade")]
    public float fadeInDuration = 1f;   // Dauer fürs Einblenden am Start

    private Rigidbody rb;

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeInDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;
            // Sinnvolle Constraints für 2.5D:
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 2f;
            rb.angularDamping = 999f;
        }

        // speed aus AgentStats ziehen (falls vorhanden)
        var stats = GetComponent<AgentStats>();
        if (stats)
        {
            speed = stats.CurrentSpeed;
            size = stats.size;
        }
        else
        {
            speed = 2f;
            size = 5f;
        }
        Debug.Log($"Player speed: {speed}, size: {size}");

        // Sanftes Fade-In (falls zugewiesen)
        if (fadeGroup)
        {
            fadeGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
    }

    // ... oben alles wie bei dir ...

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput   = Input.GetAxis("Vertical");
        bool  spacePressed    = Input.GetKeyDown(KeyCode.Space);

        // falls AgentStats Größe/Speed dynamisch ändern:
        var stats = GetComponent<AgentStats>();
        if (stats) speed = stats.CurrentSpeed;

        Vector3 targetVelocity = new Vector3(horizontalInput, verticalInput, 0f).normalized * speed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);

        // <<< NEU: harte Obergrenze inkl. kleinem Push-Bonus >>>
        float pushAllowance = 1.0f; // wie viel fremder Schub extra erlaubt ist
        float maxAllowed = speed + pushAllowance;
        if (velocity.magnitude > maxAllowed) velocity = velocity.normalized * maxAllowed;

        if (rb) rb.MovePosition(rb.position + velocity * Time.deltaTime);

        if (horizontalInput == 0f && verticalInput == 0f)
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

        // Tilt + Flip (wie bei dir)
        float targetTiltZ  =  verticalInput * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);
        float targetY = transform.eulerAngles.y;
        if (horizontalInput > 0f)      targetY = 0f;
        else if (horizontalInput < 0f) targetY = 180f;
        transform.rotation = Quaternion.Euler(0f, targetY, currentTiltZ);

        if (spacePressed)
        {
            if (animatorScript) animatorScript.PlayEatAnimation();
            if (audioSource && !audioSource.isPlaying) audioSource.Play();
        }
    }

    void OnCollisionEnter(Collision c)
    {
        // Velocity nach Stoß dämpfen
        rb.linearVelocity *= 0.6f; // <--- statt linearVelocity
    }
}