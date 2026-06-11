using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Controller VR Box/VR Park/VR Shinecon - Bluetooth Remote
/// Analog stick di controller ini SERING mengirim sinyal sebagai:
/// - Keyboard Arrow Keys (Up/Down/Left/Right)
/// - DPAD pada Gamepad/Joystick
/// - BUKAN sebagai analog axis yang sebenarnya!
/// Script ini menangani SEMUA kemungkinan tersebut.
/// </summary>
public class MotionObjectController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    public Transform playerTransform;

    [Header("Pengaturan Joystick (Generic)")]
    [Tooltip("Centang jika jalan maju/mundur malah jadi menyamping")]
    public bool swapAxis = false;
    [Tooltip("Centang jika jalan kiri/kanan terbalik")]
    public bool invertHorizontal = false;
    [Tooltip("Centang jika jalan maju/mundur terbalik")]
    public bool invertVertical = false;

    [Header("Debug (Matikan setelah selesai testing)")]
    public bool showDebugInfo = true;

    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private CharacterController controller;
    private string debugLog = "";

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
            velocity.y = -2f;

        // Camera rotation
        float cameraYRotation = cameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, cameraYRotation, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.1f);

        // === BACA INPUT DARI SEMUA SUMBER ===
        float x = 0f;
        float z = 0f;
        debugLog = "<b>=== VR CONTROLLER DEBUG ===</b>\n\n";

        // ──────────────────────────────────────
        // 1. KEYBOARD (Controller VR Park sering mengirim sinyal sebagai Arrow Keys!)
        //    INI ADALAH METODE PALING PENTING untuk controller VR murah.
        // ──────────────────────────────────────
        if (Keyboard.current != null)
        {
            bool arrowDetected = false;

            if (Keyboard.current.leftArrowKey.isPressed) { x -= 1f; arrowDetected = true; }
            if (Keyboard.current.rightArrowKey.isPressed) { x += 1f; arrowDetected = true; }
            if (Keyboard.current.upArrowKey.isPressed) { z += 1f; arrowDetected = true; }
            if (Keyboard.current.downArrowKey.isPressed) { z -= 1f; arrowDetected = true; }

            // WASD juga (untuk testing di PC)
            if (Keyboard.current.aKey.isPressed) { x -= 1f; arrowDetected = true; }
            if (Keyboard.current.dKey.isPressed) { x += 1f; arrowDetected = true; }
            if (Keyboard.current.wKey.isPressed) { z += 1f; arrowDetected = true; }
            if (Keyboard.current.sKey.isPressed) { z -= 1f; arrowDetected = true; }

            if (arrowDetected)
                debugLog += "<color=lime>KEYBOARD ARROW TERDETEKSI! X=" + x.ToString("F1") + " Z=" + z.ToString("F1") + "</color>\n";
        }

        // ──────────────────────────────────────
        // 2. GAMEPAD (jika controller dikenali sebagai Gamepad)
        // ──────────────────────────────────────
        if (Mathf.Abs(x) < 0.05f && Mathf.Abs(z) < 0.05f && Gamepad.current != null)
        {
            debugLog += "Gamepad: " + Gamepad.current.name + "\n";
            Vector2 left = Gamepad.current.leftStick.ReadValue();
            Vector2 right = Gamepad.current.rightStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();

            debugLog += "  LeftStick=" + left + " RightStick=" + right + " DPad=" + dpad + "\n";

            if (left.magnitude > 0.05f) { x = left.x; z = left.y; }
            else if (right.magnitude > 0.05f) { x = right.x; z = right.y; }
            else if (dpad.magnitude > 0.05f) { x = dpad.x; z = dpad.y; }
        }
        else if (Gamepad.current == null)
        {
            debugLog += "Gamepad: NULL\n";
        }

        // ──────────────────────────────────────
        // 3. JOYSTICK HID (jika dikenali sebagai Joystick generik)
        // ──────────────────────────────────────
        if (Mathf.Abs(x) < 0.05f && Mathf.Abs(z) < 0.05f && Joystick.current != null)
        {
            debugLog += "Joystick: " + Joystick.current.name + "\n";
            Vector2 joyVal = Joystick.current.stick.ReadValue();
            debugLog += "  Stick=" + joyVal + "\n";

            if (joyVal.magnitude > 0.05f) { x = joyVal.x; z = joyVal.y; }

            // Scan semua axis
            if (Mathf.Abs(x) < 0.05f && Mathf.Abs(z) < 0.05f)
            {
                foreach (var ctrl in Joystick.current.allControls)
                {
                    if (ctrl is AxisControl axis && !(ctrl is ButtonControl))
                    {
                        float val = axis.ReadValue();
                        if (Mathf.Abs(val) > 0.1f)
                        {
                            debugLog += "  Axis: " + axis.name + "=" + val.ToString("F2") + "\n";
                            string n = axis.name.ToLower();
                            if (n.Contains("x")) x = val;
                            else if (n.Contains("y")) z = val;
                        }
                    }
                }
            }
        }
        else if (Joystick.current == null)
        {
            debugLog += "Joystick: NULL\n";
        }

        // ──────────────────────────────────────
        // 4. SCAN SEMUA DEVICE LAIN
        // ──────────────────────────────────────
        if (Mathf.Abs(x) < 0.05f && Mathf.Abs(z) < 0.05f)
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad || device is Joystick || device is Keyboard || device is Mouse || device is Touchscreen)
                    continue;

                debugLog += "Device: " + device.name + " [" + device.GetType().Name + "]\n";

                foreach (var ctrl in device.allControls)
                {
                    if (ctrl is StickControl stick)
                    {
                        Vector2 val = stick.ReadValue();
                        if (val.magnitude > 0.05f) { x = val.x; z = val.y; break; }
                    }
                    else if (ctrl is AxisControl axis && !(ctrl is ButtonControl))
                    {
                        float val = axis.ReadValue();
                        if (Mathf.Abs(val) > 0.1f)
                            debugLog += "  " + axis.name + "=" + val.ToString("F2") + "\n";
                    }
                }
            }
        }

        // ──────────────────────────────────────
        // 5. LEGACY INPUT (jika Active Input Handling = Both)
        // ──────────────────────────────────────
        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Abs(x) < 0.05f && Mathf.Abs(z) < 0.05f)
        {
            try
            {
                float lh = Input.GetAxis("Horizontal");
                float lv = Input.GetAxis("Vertical");
                if (Mathf.Abs(lh) > 0.05f || Mathf.Abs(lv) > 0.05f)
                {
                    x = lh; z = lv;
                    debugLog += "<color=cyan>Legacy Input: H=" + lh.ToString("F2") + " V=" + lv.ToString("F2") + "</color>\n";
                }
            }
            catch { }
        }
        #endif

        // === KOREKSI ARAH ===
        if (swapAxis) { float temp = x; x = z; z = temp; }
        if (invertHorizontal) x = -x;
        if (invertVertical) z = -z;

        debugLog += "\n<b>INPUT AKHIR: X=" + x.ToString("F2") + " Z=" + z.ToString("F2") + "</b>\n";
        debugLog += "\nSemua devices:\n";
        foreach (var d in InputSystem.devices)
            debugLog += "  " + d.name + " [" + d.GetType().Name + "]\n";

        // === GERAKKAN KARAKTER ===
        moveDirection = transform.right * x + transform.forward * z;
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Sync player position
        if (playerTransform != null)
            playerTransform.position = transform.position;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 20;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;
        style.richText = true;

        float w = Screen.width * 0.55f;
        float h = Screen.height * 0.65f;
        GUI.backgroundColor = new Color(0, 0, 0, 0.9f);
        GUI.Box(new Rect(10, 10, w, h), debugLog, style);
    }
}
