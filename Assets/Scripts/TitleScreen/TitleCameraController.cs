using UnityEngine;

public class TitleCameraController : MonoBehaviour
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
    void Update()
    {
        if (!player) return;
        transform.position = player.transform.position + offset; 
    }
}
