using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public float followSpeed = 2f;
    private Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!player) return;
        Vector3 desired = player.transform.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
    }
}
