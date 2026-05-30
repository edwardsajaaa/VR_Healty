using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        doorCollider = GetComponent<Collider>();
        closedRotation = transform.localEulerAngles;
        currentRotationZ = closedRotation.z;
        targetRotationZ = currentRotationZ;
    }

    void Update()
    {
        if (isRotating)
            RotateDoor();

        CheckPlayerGaze();
        
        // Interaksi dengan tap layar (touch/klik) saat sedang melihat pintu
        if (isBeingLookedAt && DetectScreenTap())
            ToggleDoor();
    }

    /// <summary>
    /// Mendeteksi tap layar (sentuh di Android, klik kiri di Editor).
    /// Mengabaikan tap yang mengenai UI element.
    /// </summary>
    private bool DetectScreenTap()
    {
        // Cek touch di Android
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return false;
                return true;
            }
        }

        // Fallback: klik kiri mouse (untuk testing di Editor)
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        return false;
    }

    private void CheckPlayerGaze()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isBeingLookedAt)
                    isBeingLookedAt = true;
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

    private void RotateDoor()
    {
        currentRotationZ = Mathf.Lerp(currentRotationZ, targetRotationZ, Time.deltaTime * rotationSpeed);
        
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.z = currentRotationZ;
        transform.localEulerAngles = eulerAngles;
        
        if (Mathf.Abs(currentRotationZ - targetRotationZ) < rotationThreshold)
        {
            currentRotationZ = targetRotationZ;
            Vector3 finalEuler = transform.localEulerAngles;
            finalEuler.z = targetRotationZ;
            transform.localEulerAngles = finalEuler;
            isRotating = false;
        }
    }

    public void ToggleDoor()
    {
        if (isRotating) return;
        
        isOpen = !isOpen;
        targetRotationZ = isOpen ? (closedRotation.z + openAngle) : closedRotation.z;
        isRotating = true;
    }

    public void OpenDoor()
    {
        if (isOpen || isRotating) return;
        
        isOpen = true;
        targetRotationZ = closedRotation.z + openAngle;
        isRotating = true;
    }

    public void CloseDoor()
    {
        if (!isOpen || isRotating) return;
        
        isOpen = false;
        targetRotationZ = closedRotation.z;
        isRotating = true;
    }

    public bool IsOpen() { return isOpen; }
    public bool IsRotating() { return isRotating; }

    public void SetOpenAngle(float angle) { openAngle = angle; }
    public void SetRotationSpeed(float speed) { rotationSpeed = speed; }
}

