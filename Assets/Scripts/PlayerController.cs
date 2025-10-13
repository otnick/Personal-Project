using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
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
        }

        // Sanftes Fade-In (falls zugewiesen)
        if (fadeGroup)
        {
            fadeGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        // movement on x and y axes
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput   = Input.GetAxis("Vertical");
        bool  spacePressed    = Input.GetKeyDown(KeyCode.Space);

        Vector3 targetVelocity = new Vector3(horizontalInput, verticalInput, 0f).normalized * speed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        if (rb) rb.MovePosition(rb.position + velocity * Time.deltaTime);

        // decelerate when no input is given
        if (horizontalInput == 0f && verticalInput == 0f)
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

        // --- Tilt (Z) schlicht nach verticalInput ---
        float targetTiltZ  =  verticalInput * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);

        // --- Flip (Y) nur bei Links/Rechts-Input ---
        float targetY = transform.eulerAngles.y; // beibehalten, wenn keine Eingabe
        if (horizontalInput > 0f)      targetY = 0f;
        else if (horizontalInput < 0f) targetY = 180f;

        // eine Rotation setzen (Y = Flip, Z = Tilt)
        transform.rotation = Quaternion.Euler(0f, targetY, currentTiltZ);

        // eating action (nur wenn Referenzen vorhanden)
        if (spacePressed)
        {
            if (animatorScript) animatorScript.PlayEatAnimation();
            if (audioSource && !audioSource.isPlaying) audioSource.Play();
        }
    }

    // -------- helpers --------
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
}
