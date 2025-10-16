using UnityEngine;

public class TitleScreenController : MonoBehaviour
{
    public float speed = 0.5f;
    public float acceleration = 2f;
    Vector3 velocity;

    public float tiltMaxDeg = 10f;
    public float tiltLerp = 2f;

    public AnimatorScript animatorScript;
    public AudioSource audioSource;

    bool transitioning;

    void Update()
    {
        if (transitioning) return;

        // wave like movement
        float dt = Time.deltaTime;
        Vector3 targetVel = Vector3.right * speed;
        velocity = Vector3.MoveTowards(velocity, targetVel, acceleration * dt);
        transform.position += velocity * dt;

        float wave = Mathf.Sin(Time.time * 2f) * 0.02f;
        transform.position += new Vector3(0f, wave * dt, 0f);

        float targetTiltZ = -wave * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * dt);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltZ);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (audioSource && !audioSource.isPlaying) audioSource.Play();
            StartCoroutine(DiveThenGoToGame());
        }
    }

    System.Collections.IEnumerator DiveThenGoToGame()
    {
        transitioning = true;

        // dive animation before fade
        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + new Vector3(0.5f, -0.2f, 0f);
        Vector3 startScale = transform.localScale;
        Vector3 finalScale = startScale * 0.9f;

        float t = 0f, dur = 0.6f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float e = Mathf.SmoothStep(0f, 1f, t);
            transform.position   = Vector3.Lerp(startPos, endPos, e);
            transform.localScale = Vector3.Lerp(startScale, finalScale, e);
            transform.rotation   = Quaternion.Euler(0f, -90f, Mathf.Lerp(0f, -tiltMaxDeg * 0.6f, e));
            yield return null;
        }

        GameController.Instance?.ToGame();
    }
}
