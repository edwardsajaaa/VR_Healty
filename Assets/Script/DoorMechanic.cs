using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorMechanic : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float interactionDistance = 2f;
    
    [Header("Door State")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isRotating = false;
    [SerializeField] private bool isBeingLookedAt = false;
    
    private float targetRotationZ = 0f;
    private float currentRotationZ = 0f;
    private Collider doorCollider;
    private float rotationThreshold = 0.5f;
    private Vector3 closedRotation;

    void Start()
    {
        // Get door collider untuk interaction
        doorCollider = GetComponent<Collider>();
        
        // Store initial rotation (closed position)
        closedRotation = transform.localEulerAngles;
        currentRotationZ = closedRotation.z;
        targetRotationZ = currentRotationZ;
        
        Debug.Log("DoorMechanic initialized. Initial rotation Z: " + currentRotationZ);
    }

    void Update()
    {
        // Handle door rotation
        if (isRotating)
        {
            RotateDoor();
        }

        // Check if player is looking at door
        CheckPlayerGaze();
        
        // Check if E key pressed while looking at door
        if (isBeingLookedAt && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed while looking at door - Toggling door");
            ToggleDoor();
        }
    }

    /// <summary>
    /// Raycast dari camera untuk check jika player menatap pintu
    /// </summary>
    private void CheckPlayerGaze()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Raycast dari center of screen
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // Cek apakah raycast mengenai door collider ini dalam range
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                // Player sedang menatap pintu dan dalam range
                if (!isBeingLookedAt)
                {
                    isBeingLookedAt = true;
                    Debug.Log("Door in focus! Press E to interact");
                }
            }
            else
            {
                isBeingLookedAt = false;
            }
        }
        else
        {
            isBeingLookedAt = false;
        }
    }

    /// <summary>
    /// Putar pintu menuju target rotation
    /// </summary>
    private void RotateDoor()
    {
        currentRotationZ = Mathf.Lerp(currentRotationZ, targetRotationZ, Time.deltaTime * rotationSpeed);
        
        // Apply rotation langsung ke door GameObject (Z axis)
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.z = currentRotationZ;
        transform.localEulerAngles = eulerAngles;
        
        // Cek apakah sudah mencapai target
        if (Mathf.Abs(currentRotationZ - targetRotationZ) < rotationThreshold)
        {
            currentRotationZ = targetRotationZ;
            Vector3 finalEuler = transform.localEulerAngles;
            finalEuler.z = targetRotationZ;
            transform.localEulerAngles = finalEuler;
            
            isRotating = false;
            Debug.Log("Door rotation complete. Door is " + (isOpen ? "OPEN" : "CLOSED"));
        }
    }

    /// <summary>
    /// Toggle pintu antara terbuka dan tertutup
    /// </summary>
    public void ToggleDoor()
    {
        if (isRotating)
        {
            Debug.Log("Door is already rotating!");
            return;
        }
        
        isOpen = !isOpen;
        targetRotationZ = isOpen ? (closedRotation.z + openAngle) : closedRotation.z;
        isRotating = true;
        
        Debug.Log("Door toggle - Opening: " + isOpen + ", Target angle: " + targetRotationZ);
    }

    /// <summary>
    /// Buka pintu
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen || isRotating) return;
        
        isOpen = true;
        targetRotationZ = closedRotation.z + openAngle;
        isRotating = true;
        
        Debug.Log("Opening door...");
    }

    /// <summary>
    /// Tutup pintu
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen || isRotating) return;
        
        isOpen = false;
        targetRotationZ = closedRotation.z;
        isRotating = true;
        
        Debug.Log("Closing door...");
    }

    /// <summary>
    /// Cek apakah pintu sedang terbuka
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// Cek apakah pintu sedang bergerak
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }

    /// <summary>
    /// Set sudut pembukaan pintu
    /// </summary>
    public void SetOpenAngle(float angle)
    {
        openAngle = angle;
        Debug.Log("Open angle set to: " + angle);
    }

    /// <summary>
    /// Set kecepatan rotasi
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
        Debug.Log("Rotation speed set to: " + speed);
    }
}
