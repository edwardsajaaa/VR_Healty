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

    [Header("Keyboard Walk Tester (Laptop / PC)")]
    [Tooltip("Aktifkan mode tester keyboard (WASD/Arrow + Q/E putar badan + Shift lari) saat tanpa controller")]
    public bool enableKeyboardTestMode = true;
    [Tooltip("Kecepatan lari saat menahan tombol Shift di keyboard")]
    public float keyboardSprintMultiplier = 1.8f;
    [Tooltip("Kecepatan putar player/kamera menggunakan tombol Q dan E di keyboard")]
    public float keyboardTurnSpeed = 90.0f;
    [Tooltip("Tampilkan panel info/debug UI di layar laptop saat menggunakan keyboard tester")]
    public bool showKeyboardWalkUI = true;

    private bool isMovementLocked = false;
    private Transform camTransform;
    private CharacterController controller;

    private Canvas testerCanvas;
    private UnityEngine.UI.Text testerTextUI;
    private bool uiVisible = true;

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

        if (enableKeyboardTestMode && showKeyboardWalkUI)
        {
            CreateWalkTesterUI();
        }
    }

    private void CreateWalkTesterUI()
    {
        // Hanya buat UI di mode Editor atau Laptop Desktop agar tidak mengganggu pandangan VR di headset
        if (!Application.isEditor && SystemInfo.deviceType != DeviceType.Desktop) return;

        GameObject canvasObj = new GameObject("KeyboardWalkTester_UI");
        testerCanvas = canvasObj.AddComponent<Canvas>();
        testerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testerCanvas.sortingOrder = 990;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject bgObj = new GameObject("TesterPanel");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 1);
        bgRT.anchorMax = new Vector2(0, 1);
        bgRT.pivot = new Vector2(0, 1);
        bgRT.anchoredPosition = new Vector2(20, -20);
        bgRT.sizeDelta = new Vector2(430, 245);

        GameObject txtObj = new GameObject("TesterText");
        txtObj.transform.SetParent(bgObj.transform, false);
        testerTextUI = txtObj.AddComponent<UnityEngine.UI.Text>();
        testerTextUI.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        testerTextUI.fontSize = 15;
        testerTextUI.color = Color.white;
        testerTextUI.alignment = TextAnchor.UpperLeft;
        testerTextUI.supportRichText = true;

        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(15, 15);
        txtRT.offsetMax = new Vector2(-15, -15);
    }

    void Update()
    {
        if (isMovementLocked || camTransform == null)
        {
            isWalking = false;
            if (testerCanvas != null) testerCanvas.enabled = false;
            return;
        }
        
        // Baca nilai Vector2 dari Input Action (VR Controller / Joystick)
        Vector2 inputMove = moveAction.ReadValue<Vector2>();
        
        // Cek langsung dari Keyboard laptop (Tester Mode & Fallback) agar 100% selalu berfungsi di Editor/Laptop
        Vector2 keyboardInput = Vector2.zero;
        float turnInput = 0f;
        bool isSprinting = false;

        if (enableKeyboardTestMode)
        {
            float kh = 0f;
            float kv = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) kv += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) kv -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kh += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kh -= 1f;

                if (Keyboard.current.qKey.isPressed) turnInput -= 1f;
                if (Keyboard.current.eKey.isPressed) turnInput += 1f;

                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) isSprinting = true;
                if (Keyboard.current.f1Key.wasPressedThisFrame) uiVisible = !uiVisible;
            }

            // Fallback ke Legacy Input System jika New Input System nilainya 0 (aman di semua mode Project Settings Unity)
            if (Mathf.Abs(kh) < 0.01f && Mathf.Abs(kv) < 0.01f)
            {
                try
                {
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) kv += 1f;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) kv -= 1f;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kh += 1f;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kh -= 1f;
                }
                catch { }
            }

            if (turnInput == 0f)
            {
                try
                {
                    if (Input.GetKey(KeyCode.Q)) turnInput -= 1f;
                    if (Input.GetKey(KeyCode.E)) turnInput += 1f;
                    if (Input.GetKeyDown(KeyCode.F1)) uiVisible = !uiVisible;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) isSprinting = true;
                }
                catch { }
            }

            keyboardInput = new Vector2(Mathf.Clamp(kh, -1f, 1f), Mathf.Clamp(kv, -1f, 1f));

            // Rotasi arah kamera / player menggunakan tombol Q dan E (sangat praktis di laptop tanpa mouse eksternal)
            if (turnInput != 0f)
            {
                transform.Rotate(Vector3.up * turnInput * keyboardTurnSpeed * Time.deltaTime);
            }
        }

        // Ambil input dominan antara Joystick VR dengan Keyboard Laptop
        float horizontal = Mathf.Abs(inputMove.x) > Mathf.Abs(keyboardInput.x) ? inputMove.x : keyboardInput.x;
        float vertical = Mathf.Abs(inputMove.y) > Mathf.Abs(keyboardInput.y) ? inputMove.y : keyboardInput.y;

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

        float currentSpeed = speed * (isSprinting && isWalking ? keyboardSprintMultiplier : 1.0f);

        if (isWalking)
        {
            Move(horizontal, vertical, currentSpeed);
        }

        if (enableKeyboardTestMode && showKeyboardWalkUI)
        {
            UpdateWalkTesterUI(horizontal, vertical, isSprinting, currentSpeed);
        }
    }

    void Move(float horizontal, float vertical, float currentSpeed = -1f)
    {
        if (currentSpeed < 0f) currentSpeed = speed;

        // Arah pergerakan relatif terhadap arah pandangan kamera (headset VR / kamera laptop)
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
        controller.SimpleMove(moveDirection * currentSpeed);
    }
    
    private void UpdateWalkTesterUI(float horizontal, float vertical, bool isSprinting, float currentSpeed)
    {
        if (testerCanvas == null || testerTextUI == null) return;

        testerCanvas.enabled = uiVisible && showKeyboardWalkUI && enableKeyboardTestMode;
        if (!testerCanvas.enabled) return;

        string statusColor = isWalking ? "#00FF66" : "#AAAAAA";
        string statusText = isWalking ? (isSprinting ? "🏃 BERLARI (SPRINT)" : "🚶 BERJALAN") : "⏸️ DIAM (IDLE)";

        string text = "<b><size=17><color=#55BBFF>⌨️ KEYBOARD WALK TESTER (LAPTOP)</color></size></b>\n";
        text += "<color=#CCCCCC>Simulasi jalan tanpa VR Controller</color>\n\n";

        text += $"<b>Status :</b> <color={statusColor}><b>{statusText}</b></color>\n";
        text += $"<b>Input Vector :</b> X: {horizontal:F2} | Y: {vertical:F2}\n";
        text += $"<b>Kecepatan :</b> {currentSpeed:F1} {(isSprinting ? "<color=yellow>(x" + keyboardSprintMultiplier + " Shift)</color>" : "")}\n\n";

        text += "<color=#FFFF88><b>Panduan Kontrol Laptop:</b></color>\n";
        text += "• <b>[W][A][S][D] / [Panah]</b> : Maju, Mundur, Kiri, Kanan\n";
        text += "• <b>[Q] / [E]</b> : Putar Badan (Turn Left / Right)\n";
        text += "• <b>[Shift]</b> : Lari Cepat (Sprint)\n";
        text += "• <b>[Klik Kanan / Alt + Mouse]</b> : Menengok (Mouse Look)\n";
        text += "• <b>[F1]</b> : Sembunyikan/Tampilkan Panel UI Ini";

        testerTextUI.text = text;
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