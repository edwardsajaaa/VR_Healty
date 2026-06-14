using UnityEngine;

public class MotionObjectController : MonoBehaviour
{
    public float moveSpeed = 5f;    // Speed of movement
    public float jumpHeight = 2f;  // Height of the jump
    public float gravity = -9.81f; // Gravity force
    public Transform cameraTransform; // Reference to the main camera
    public Transform playerTransform; // Reference to the player

    [Header("Pengaturan Joystick (Generic)")]
    [Tooltip("Centang jika jalan maju/mundur malah jadi menyamping")]
    public bool swapAxis = false;
    [Tooltip("Centang jika jalan kiri/kanan terbalik")]
    public bool invertHorizontal = false;
    [Tooltip("Centang jika jalan maju/mundur terbalik")]
    public bool invertVertical = false;

    private Vector3 moveDirection;  // For movement
    private Vector3 velocity;       // For gravity and jumping
    private bool isGrounded;        // Check if grounded

    private CharacterController controller; // For motion collision

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            Debug.LogError("CharacterController component is missing!");
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded
        }

        // Get camera's Y rotation
        float cameraYRotation = cameraTransform.eulerAngles.y;

        // Smoothly align the motion object's rotation with the camera
        Quaternion targetRotation = Quaternion.Euler(0, cameraYRotation, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.1f);

        // === KEMBALI MENGGUNAKAN LEGACY INPUT MANAGER ===
        // Ini adalah cara paling ampuh untuk controller generic VR Park di Android
        float x = Input.GetAxis("Horizontal"); // Sideways movement
        float z = Input.GetAxis("Vertical");   // Forward/backward movement

        // Koreksi arah dari Inspector (Penting untuk VR Park Controller)
        if (swapAxis)
        {
            float temp = x;
            x = z;
            z = temp;
        }
        if (invertHorizontal) x = -x;
        if (invertVertical) z = -z;

        // Calculate movement direction relative to motion object's orientation
        moveDirection = transform.right * x + transform.forward * z;

        // Apply movement
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update the player's position and rotation to match the motion object
        if (playerTransform != null)
        {
            playerTransform.position = transform.position;
        }
    }
}
