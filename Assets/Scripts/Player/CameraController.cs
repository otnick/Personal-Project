using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public float followSpeed = 2f;
    private Vector3 offset;

    // Refs for fade in
    public CanvasGroup fadeGroup;
    public float fadeInDuration = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - player.transform.position;
        if (fadeGroup) { fadeGroup.alpha = 1f; StartCoroutine(FadeIn()); }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!player) return;
        Vector3 desired = player.transform.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

        // Smooth zoom effect when boosting
        float targetSize = player.GetComponent<PlayerController>().boosting ? 6f : 5f;
        Camera cam = GetComponent<Camera>();
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 3f * Time.deltaTime);
    }

    // fade in
    public System.Collections.IEnumerator FadeIn()
    {
        float t = 0f, dur = Mathf.Max(0.01f, fadeInDuration);
        while (t < dur) { t += Time.deltaTime; fadeGroup.alpha = 1f - Mathf.Clamp01(t / dur); yield return null; }
        fadeGroup.alpha = 0f;
    }
}
