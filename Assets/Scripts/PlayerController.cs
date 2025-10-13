using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    private Vector3 velocity = Vector3.zero;
    public float tiltMaxDeg = 40f;    // maximaler Neigungswinkel (hoch/runter)
    public float tiltLerp = 36f;      // wie schnell ins Ziel kippen
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

        // decelerate when no input is given
        if (horizontalInput == 0 && verticalInput == 0)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        // --- Tilt (Z) schlicht nach verticalInput ---
        float targetTiltZ = -verticalInput * tiltMaxDeg;
        float currentTiltZ = Mathf.LerpAngle(transform.eulerAngles.z, targetTiltZ, tiltLerp * Time.deltaTime);

        // --- Flip (Y) nur bei Links/Rechts-Input ---
        float targetY = transform.eulerAngles.y; // beibehalten, wenn keine Eingabe
        if (horizontalInput > 0f)      targetY = 180f;
        else if (horizontalInput < 0f) targetY = 0f;

        // eine Rotation setzen (Y = Flip, Z = Tilt)
        transform.rotation = Quaternion.Euler(0f, targetY, currentTiltZ);
    }
}
