using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class VRWalkController : MonoBehaviour
{
    [Header("Pengaturan Jalan")]
    public float speed = 3.0f;
    public bool isWalking = false;
    
    [Header("Pengaturan Joystick (Generic)")]
    [Tooltip("Centang jika jalan maju/mundur malah jadi menyamping")]
    public bool swapAxis = false;
    [Tooltip("Centang jika jalan kiri/kanan terbalik")]
    public bool invertHorizontal = false;
    [Tooltip("Centang jika jalan maju/mundur terbalik")]
    public bool invertVertical = false;

    private bool isMovementLocked = false;
    private Transform camTransform;
    private CharacterController controller;

    [Header("Input System (Otomatis)")]
    [Tooltip("Tidak perlu diisi, otomatis mendeteksi VR Controller & Keyboard")]
    public InputAction moveAction = new InputAction("Move", InputActionType.Value, "Vector2");

    void Awake()
    {
        // Setup binding default agar bisa langsung jalan di VR (XR Controller) dan PC (Keyboard WASD/Gamepad)
        if (moveAction.bindings.Count == 0)
        {
            // XR Controller (VR)
            moveAction.AddBinding("<XRController>/joystick");
            moveAction.AddBinding("<XRController>/primary2DAxis");
            moveAction.AddBinding("<XRController>/trackpad");
            
            // Gamepad biasa (XBox/PS)
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            // Keyboard WASD
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
                
            // Keyboard Arrow Keys
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (isMovementLocked || camTransform == null)
        {
            isWalking = false;
            return;
        }
        
        // Baca nilai Vector2 dari Input Action yang sudah kita setup di Awake
        Vector2 inputMove = moveAction.ReadValue<Vector2>();
        float horizontal = inputMove.x;
        float vertical = inputMove.y;

        // Koreksi arah jika diperlukan (berguna untuk controller yang dipegang horizontal/vertikal)
        if (swapAxis)
        {
            float temp = horizontal;
            horizontal = vertical;
            vertical = temp;
        }
        if (invertHorizontal) horizontal = -horizontal;
        if (invertVertical) vertical = -vertical;
        
        isWalking = Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f;

        if (isWalking)
        {
            Move(horizontal, vertical);
        }
    }

    void Move(float horizontal, float vertical)
    {
        // Arah pergerakan relatif terhadap arah pandangan kamera (headset VR)
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;

        // Abaikan kemiringan kamera pada sumbu Y agar pergerakan tetap mendatar
        forward.y = 0;
        right.y = 0;
        
        forward.Normalize(); 
        right.Normalize();

        // Hitung arah gerak berdasarkan input dan arah pandangan
        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        // Gerakkan karakter menggunakan SimpleMove
        controller.SimpleMove(moveDirection * speed);
    }
    
    public void LockMovement()
    {
        isMovementLocked = true;
        isWalking = false;
    }
    
    public void UnlockMovement()
    {
        isMovementLocked = false;
    }
}