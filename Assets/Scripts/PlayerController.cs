using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // movement on x and y axes
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 targetVelocity = new Vector3(horizontalInput, verticalInput, 0).normalized * speed;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        rb.MovePosition(rb.position + velocity * Time.deltaTime);

    }
}
