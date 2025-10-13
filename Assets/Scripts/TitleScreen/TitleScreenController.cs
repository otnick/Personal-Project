using UnityEngine;
using UnityEngine.SceneManagement; // <- wichtig für Scene-Wechsel

public class TitleScreenController : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 2f;
    public float deceleration = 1f;
    private Vector3 velocity = Vector3.zero;
    public float tiltMaxDeg = 10f;   // kleiner, subtiler Effekt
    public float tiltLerp = 2f;
    public AnimatorScript animatorScript;
    public AudioSource audioSource;

    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Bewegung: dauerhaft leicht nach rechts
        Vector3 targetVelocity = Vector3.right * speed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        rb.MovePosition(rb.position + velocity * Time.deltaTime);

        // Leichte Auf/Ab-Bewegung (optische "Schwimmbewegung")
        float wave = Mathf.Sin(Time.time * 2f) * 0.005f; // kleine Welle auf Y-Achse
        rb.MovePosition(rb.position + new Vector3(0, wave, 0));

        // Subtiler Tilt-Effekt (Z-Rotation folgt der Welle)
        float targetTiltZ = -wave * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltZ);

        // Space = Eat + Scenewechsel
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (animatorScript != null)
                animatorScript.PlayEatAnimation();

            if (audioSource != null && !audioSource.isPlaying)
                audioSource.Play();

            // Kleines Delay, bevor Szene wechselt (z. B. um Animation abspielen zu lassen)
            Invoke(nameof(LoadRiffScene), 0.6f);
        }
    }

    void LoadRiffScene()
    {
        SceneManager.LoadScene("Riff"); // Name exakt wie im Build Settings!
    }
}
