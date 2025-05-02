using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 150f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseRotation();
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;

        if (move.magnitude > 0.1f)
        {
            Vector3 moveDirection = transform.TransformDirection(move) * moveSpeed;
            rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
        }
        else
        {
            // Kein Input → Bewegung stoppen (X/Z)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        //float moveZ = Input.GetAxis("Vertical");
        //float moveX = Input.GetAxis("Horizontal");

        //Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;
        //Vector3 moveDirection = transform.TransformDirection(move) * moveSpeed;

        //rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
    }

    void HandleMouseRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");

        transform.Rotate(0f, mouseX * rotationSpeed * Time.deltaTime, 0f);
    }
}
