using UnityEngine;
using UnityEngine.SceneManagement; // <- wichtig für Scene-Wechsel

public class TitleScreenController : MonoBehaviour
{
    public float speed = 0.5f;
    public float acceleration = 2f;
    public float deceleration = 1f;
    private Vector3 velocity = Vector3.zero;
    public float tiltMaxDeg = 10f;   // kleiner, subtiler Effekt
    public float tiltLerp = 2f;
    public AnimatorScript animatorScript;
    public AudioSource audioSource;
    // Fade-Out-Effekt
    public CanvasGroup fadeGroup;     // schwarzes Overlay (alpha=0)
    public float diveDuration = 0.8f; // Zeit fürs "Reinschwimmen"
    public float fadeDelay = 0.2f;    // wann das Fade startet (nach Beginn Dive)
    public float fadeDuration = 1f; // Zeit fürs Schwarzblenden
    public float diveDepth = 5f;      // wie weit nach hinten (Z+)
    public float endScale = 0.6f;     // wie klein am Ende

    private Rigidbody rb;
    private bool transitioning;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        if (fadeGroup) fadeGroup.alpha = 0f;
    }

    void Update()
    {
        if (transitioning) return;
        // Bewegung: dauerhaft leicht nach rechts
        Vector3 targetVelocity = Vector3.right * speed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        rb.MovePosition(rb.position + velocity * Time.deltaTime);

        // Leichte Auf/Ab-Bewegung (optische "Schwimmbewegung")
        float wave = Mathf.Sin(Time.time * 2f) * 0.0005f; // kleine Welle auf Y-Achse
        rb.MovePosition(rb.position + new Vector3(0, wave, 0));

        // Subtiler Tilt-Effekt (Z-Rotation folgt der Welle)
        float targetTiltZ = -wave * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltZ);

        // Space = Eat + Scenewechsel
         if (Input.GetKeyDown(KeyCode.Space))
        {
            if (animatorScript) animatorScript.PlayEatAnimation();
            if (audioSource && !audioSource.isPlaying) audioSource.Play();
            StartCoroutine(DiveAndFade());
        }
    }

     System.Collections.IEnumerator DiveAndFade()
    {
        transitioning = true;

        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + new Vector3(0.5f, 0f, diveDepth);  // leicht nach rechts + in die Tiefe
        Vector3 startScale = transform.localScale;
        Vector3 finalScale = startScale * endScale;

        float t = 0f;
        float fadeT = 0f;

        // Dive + Fade überlappen
        while (t < 1f || (fadeGroup && fadeGroup.alpha < 1f))
        {
            // Dive (0..1)
            if (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, diveDuration);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                transform.position   = Vector3.Lerp(startPos, endPos, ease);
                transform.localScale = Vector3.Lerp(startScale, finalScale, ease);
                // Optional: etwas stärkerer Tilt beim Abtauchen
                float diveTilt = Mathf.LerpAngle(transform.eulerAngles.z, -tiltMaxDeg * 0.6f, Time.deltaTime * 4f);
                transform.rotation = Quaternion.Euler(0f, -90f, diveTilt);
            }

            // Fade
            if (fadeGroup)
            {
                if (t >= fadeDelay / Mathf.Max(0.0001f, diveDuration)) // nach kurzer Verzögerung starten
                    fadeT += Time.deltaTime / Mathf.Max(0.0001f, fadeDuration);

                fadeGroup.alpha = Mathf.Clamp01(fadeT);
            }

            yield return null;
        }

        SceneManager.LoadScene("Riff");
    }
}
