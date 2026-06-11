using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class VRWalkController : MonoBehaviour
{
    [Header("Pengaturan Jalan")]
    public float speed = 3.0f;
    public bool isWalking = false;
    
    private bool isMovementLocked = false;
    private Transform camTransform;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        if (isMovementLocked)
        {
            isWalking = false;
            return;
        }
        
        float horizontal = 0f;
        float vertical = 0f;

        // Baca input dari Gamepad (Controller VR)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            horizontal = stick.x;
            vertical = stick.y;
        }
        // Fallback untuk pengetesan di PC/Editor menggunakan Keyboard WASD/Arrow
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
        }
        
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