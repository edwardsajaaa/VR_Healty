using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    /// Deteksi tap layar. Gaze check (isBeingLookedAt) sudah jadi filter,
    /// jadi tidak perlu IsPointerOverGameObject yang bermasalah di Cardboard Android.
    /// </summary>
    private bool DetectScreenTap()
    {
        // Controller VR Park bisa terdeteksi sebagai Gamepad, Joystick, atau device lain.
        // Kita cek SEMUA kemungkinan:

        // 1. Cek Gamepad
        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
        }

        // 2. Cek Joystick (VR Park sering terdeteksi sebagai ini!)
        if (Joystick.current != null)
        {
            foreach (var control in Joystick.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
        }

        // 3. Cek semua device lain yang terhubung
        foreach (var device in InputSystem.devices)
        {
            if (device is Gamepad || device is Joystick || device is Keyboard || device is Mouse || device is Touchscreen)
                continue;
            foreach (var control in device.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
        }

        // Android touch (New Input System)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        // Editor fallback: klik kiri mouse / Keyboard C
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            return true;

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

