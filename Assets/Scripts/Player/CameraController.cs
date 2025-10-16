using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public float followSpeed = 2f;
    private Vector3 offset;

    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    // LateUpdate to make camera movement smoother
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
}
