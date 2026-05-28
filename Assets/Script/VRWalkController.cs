using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VRWalkController : MonoBehaviour
{
    [Header("Pengaturan Jalan")]
    public float speed = 3.0f;
    public float walkThreshold = 20.0f;
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
        
        float headPitch = camTransform.eulerAngles.x;
        
        if (headPitch < (360.0f - walkThreshold) && headPitch > 270.0f)
            isWalking = true;
        else
            isWalking = false;

        if (isWalking)
            MoveForward();
    }

    void MoveForward()
    {
        Vector3 forward = camTransform.forward;
        forward.y = 0;
        forward.Normalize(); 
        controller.SimpleMove(forward * speed);
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