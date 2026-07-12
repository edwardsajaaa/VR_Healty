using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ChairSitController : MonoBehaviour
{
    [Header("Chair Settings")]
    [Tooltip("Nama kursi untuk ditampilkan pada UI (contoh: Kursi Dokter)")]
    [SerializeField] private string chairName = "Kursi Dokter";

    [Tooltip("Titik posisi di mana player akan duduk (buat GameObject kosong di kursi sebagai anak dari kursi). Jika kosong, otomatis menggunakan posisi kursi.")]
    [SerializeField] private Transform sitPoint;

    [Tooltip("Titik posisi di mana player akan berdiri setelah duduk. Jika kosong, otomatis kembali ke posisi sebelum duduk.")]
    [SerializeField] private Transform standPoint;

    [Tooltip("Offset posisi duduk jika sitPoint tidak diisi (misal Y = 0.4 agar ketinggian mata pas)")]
    [SerializeField] private Vector3 sitOffset = new Vector3(0, 0.4f, 0);

    [Header("Interaction Settings")]
    [Tooltip("Jika dicentang, player langsung otomatis duduk begitu masuk ke dalam Box Collider kursi tanpa perlu menekan tombol.")]
    [SerializeField] private bool autoSitOnTriggerEnter = false;

    [Tooltip("Jarak maksimal interaksi jika player menatap/melihat kursi menggunakan Gaze / Raycast kamera.")]
    [SerializeField] private float interactionDistance = 2.5f;

    [Header("UI Settings")]
    [SerializeField] private float canvasDistance = 0.8f;
    [SerializeField] private float canvasScale = 0.0013f;
    [SerializeField] private Color panelColor = new Color(0.11f, 0.13f, 0.17f, 0.95f);
    [SerializeField] private Color accentColor = new Color(0.35f, 0.65f, 0.95f, 1.0f);

    [Header("Input System (Otomatis)")]
    public InputAction interactAction = new InputAction("Interact", InputActionType.Button);
    public InputAction cancelAction = new InputAction("Cancel", InputActionType.Button);

    // ── State ──
    private bool isSitting = false;
    private bool isPlayerNearby = false;
    private bool isBeingLookedAt = false;
    private bool isTransitioning = false;

    private Camera mainCamera;
    private VRWalkController playerController;
    private MotionObjectController motionController;
    private Vector3 lastStandPosition;
    private Quaternion lastStandRotation;

    // ── UI References ──
    private Canvas chairCanvas;
    private GameObject sitHintPanel;
    private GameObject standHintPanel;
    private Text sitHintText;
    private Text standHintText;

    void Awake()
    {
        // Binding default otomatis (VR Controller, Gamepad, Keyboard, Mouse)
        if (interactAction.bindings.Count == 0)
        {
            interactAction.AddBinding("<XRController>/triggerPressed");
            interactAction.AddBinding("<XRController>/primaryButton"); // Tombol A
            interactAction.AddBinding("<Gamepad>/buttonSouth");        // Tombol A
            interactAction.AddBinding("<Keyboard>/c");
            interactAction.AddBinding("<Keyboard>/space");
            interactAction.AddBinding("<Mouse>/leftButton");
        }

        if (cancelAction.bindings.Count == 0)
        {
            cancelAction.AddBinding("<XRController>/secondaryButton"); // Tombol B
            cancelAction.AddBinding("<Gamepad>/buttonEast");           // Tombol B
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Keyboard>/b");
        }
    }

    void OnEnable()
    {
        interactAction.Enable();
        cancelAction.Enable();
    }

    void OnDisable()
    {
        interactAction.Disable();
        cancelAction.Disable();
    }

    void Start()
    {
        mainCamera = Camera.main;
        playerController = FindObjectOfType<VRWalkController>();

        BuildUI();
    }

    void Update()
    {
        if (mainCamera == null) return;
        if (playerController == null) playerController = FindObjectOfType<VRWalkController>();

        if (isSitting)
        {
            // Cek apakah player sedang mengarahkan pandangan ke NPC / Poster atau sedang membuka dialog
            if (IsLookingAtOrInteractingWithNPC())
            {
                // Nonaktifkan sementara UI dan Trigger berdiri agar tidak bentrok saat bicara dengan NPC
                if (standHintPanel != null && standHintPanel.activeSelf)
                {
                    standHintPanel.SetActive(false);
                }
                return; // Abaikan pengecekan tombol berdiri
            }
            else
            {
                // Aktifkan kembali UI berdiri jika tidak sedang menatap NPC
                if (standHintPanel != null && !standHintPanel.activeSelf)
                {
                    standHintPanel.SetActive(true);
                    UpdateCanvasPosition();
                }
            }

            // Update posisi UI agar selalu berada di depan pandangan saat duduk
            UpdateCanvasPosition();

            // Handle Berdiri (Tekan tombol B / Cancel / Spasi / Klik) - HANYA jika tidak menatap NPC
            bool cancelPressed = false;
            if (cancelAction != null && cancelAction.WasPressedThisFrame()) cancelPressed = true;
            if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B)) cancelPressed = true;

            // Di mobile/desktop juga bisa klik layar/mouse untuk berdiri bila UI aktif
            if (DetectScreenTapOrController()) cancelPressed = true;

            if (cancelPressed && !isTransitioning)
            {
                StandUp();
            }
            return;
        }

        // Cek apakah player dekat / melihat kursi
        CheckPlayerGazeAndDistance();

        // Tampilkan/sembunyikan hint duduk
        bool canInteract = (isPlayerNearby || isBeingLookedAt) && !isTransitioning;
        if (sitHintPanel != null)
        {
            sitHintPanel.SetActive(canInteract && !autoSitOnTriggerEnter);
            if (canInteract && !autoSitOnTriggerEnter) UpdateCanvasPosition();
        }

        // Handle Duduk via Tombol Controller / Tap
        if (canInteract && !autoSitOnTriggerEnter && !isTransitioning)
        {
            if (DetectScreenTapOrController() || (interactAction != null && interactAction.WasPressedThisFrame()))
            {
                SitDown();
            }
        }
    }

    // ── Trigger Box Collider Interaksi ──
    private void OnTriggerEnter(Collider other)
    {
        if (isSitting || isTransitioning) return;

        // Cek apakah yang masuk adalah Player (bisa deteksi via Tag, VRWalkController, MotionObjectController, atau CharacterController)
        if (other.CompareTag("Player") || other.GetComponent<VRWalkController>() != null || other.GetComponentInParent<VRWalkController>() != null || other.GetComponent<MotionObjectController>() != null || other.GetComponentInParent<MotionObjectController>() != null || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<CharacterController>() != null)
        {
            isPlayerNearby = true;

            if (autoSitOnTriggerEnter)
            {
                SitDown();
            }
            else if (sitHintPanel != null)
            {
                sitHintPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VRWalkController>() != null || other.GetComponentInParent<VRWalkController>() != null || other.GetComponent<MotionObjectController>() != null || other.GetComponentInParent<MotionObjectController>() != null || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<CharacterController>() != null)
        {
            isPlayerNearby = false;
            if (!isSitting && sitHintPanel != null)
            {
                sitHintPanel.SetActive(false);
            }
        }
    }

    private void CheckPlayerGazeAndDistance()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        if (distance <= interactionDistance)
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isBeingLookedAt = true;
                    return;
                }
            }
        }
        isBeingLookedAt = false;
    }

    // ── Cek Apakah Player Mengarah ke NPC atau Dialog Sedang Aktif ──
    private bool IsLookingAtOrInteractingWithNPC()
    {
        // 1. Cek dari static GazeDialog apakah ada NPC yang sedang ditatap atau dialognya terbuka
        if (GazeDialog.IsAnyNPCGazedOrOpen()) return true;

        // 2. Cek Raycast langsung dari kamera apakah menunjuk ke objek ber-Tag "NPC" atau objek berkomponen GazeDialog / InteractionPoster
        if (mainCamera != null)
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 8.0f))
            {
                if (hit.collider.CompareTag("NPC") || hit.collider.GetComponentInParent<GazeDialog>() != null || hit.collider.GetComponent<GazeDialog>() != null || hit.collider.GetComponentInParent<InteractionPoster>() != null || hit.collider.GetComponent<InteractionPoster>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool DetectScreenTapOrController()
    {
        // Cek dari New Input System
        if (interactAction != null && interactAction.WasPressedThisFrame()) return true;

        // Cek tombol Joystick generik (Legacy)
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i))) return true;
        }

        // Cek Android Touch
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) return true;

        // Cek Mouse / Keyboard Desktop
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space)) return true;

        return false;
    }

    // ── Helper Universal untuk Deteksi & Kunci Player ──
    private Transform GetPlayerTransform()
    {
        // 1. Cek VRWalkController
        if (playerController == null) playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null) return playerController.transform;

        // 2. Cek MotionObjectController
        if (motionController == null) motionController = FindObjectOfType<MotionObjectController>();
        if (motionController != null) return motionController.transform;

        // 3. Cek Tag "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) return playerObj.transform;

        // 4. Cek dari Camera.main
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (mainCamera.transform.parent != null) return mainCamera.transform.parent;
            return mainCamera.transform;
        }

        return null;
    }

    private void LockPlayerMovement()
    {
        if (playerController == null) playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null) playerController.LockMovement();

        if (motionController == null) motionController = FindObjectOfType<MotionObjectController>();
        if (motionController != null) motionController.LockMovement();
    }

    private void UnlockPlayerMovement()
    {
        if (playerController == null) playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null) playerController.UnlockMovement();

        if (motionController == null) motionController = FindObjectOfType<MotionObjectController>();
        if (motionController != null) motionController.UnlockMovement();
    }

    // ── Aksi Duduk & Berdiri ──
    public void SitDown()
    {
        if (isSitting || isTransitioning) return;

        Transform targetPlayer = GetPlayerTransform();
        if (targetPlayer == null)
        {
            Debug.LogWarning("Player (VRWalkController / MotionObjectController / Tag 'Player') tidak ditemukan di scene!");
            return;
        }

        StartCoroutine(SitDownCoroutine(targetPlayer));
    }

    private IEnumerator SitDownCoroutine(Transform targetPlayer)
    {
        isTransitioning = true;

        // Simpan posisi berdiri player sebelum duduk agar bisa kembali dengan tepat
        lastStandPosition = targetPlayer.position;
        lastStandRotation = targetPlayer.rotation;

        // Kunci pergerakan jalan player
        LockPlayerMovement();

        // Matikan sementara CharacterController agar tidak bentrok saat teleport posisi
        CharacterController cc = targetPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Hitung target posisi & rotasi duduk
        Vector3 targetPos = (sitPoint != null) ? sitPoint.position : (transform.position + sitOffset);
        Quaternion targetRot = (sitPoint != null) ? sitPoint.rotation : transform.rotation;

        // Teleport player ke kursi
        targetPlayer.position = targetPos;
        targetPlayer.rotation = targetRot;

        // Khusus jika menggunakan MotionObjectController, sinkronkan juga playerTransform jika terpisah
        if (motionController != null && motionController.playerTransform != null)
        {
            motionController.playerTransform.position = targetPos;
            motionController.playerTransform.rotation = targetRot;
        }

        yield return null;

        if (cc != null) cc.enabled = true;

        isSitting = true;
        isTransitioning = false;

        // Update UI
        if (sitHintPanel != null) sitHintPanel.SetActive(false);
        if (standHintPanel != null)
        {
            standHintPanel.SetActive(true);
            UpdateCanvasPosition();
        }
    }

    public void StandUp()
    {
        if (!isSitting || isTransitioning) return;

        Transform targetPlayer = GetPlayerTransform();
        if (targetPlayer == null) return;

        StartCoroutine(StandUpCoroutine(targetPlayer));
    }

    private IEnumerator StandUpCoroutine(Transform targetPlayer)
    {
        isTransitioning = true;

        // Matikan sementara CharacterController
        CharacterController cc = targetPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Hitung target posisi berdiri (kembali ke posisi awal atau titik standPoint)
        Vector3 targetPos;
        Quaternion targetRot;

        if (standPoint != null)
        {
            targetPos = standPoint.position;
            targetRot = standPoint.rotation;
        }
        else if (lastStandPosition != Vector3.zero)
        {
            targetPos = lastStandPosition;
            targetRot = lastStandRotation;
        }
        else
        {
            // Fallback: 0.8 meter di depan kursi agar tidak terjebak dalam collider kursi
            targetPos = transform.position + transform.forward * 0.8f;
            targetRot = transform.rotation;
        }

        targetPlayer.position = targetPos;
        targetPlayer.rotation = targetRot;

        if (motionController != null && motionController.playerTransform != null)
        {
            motionController.playerTransform.position = targetPos;
            motionController.playerTransform.rotation = targetRot;
        }

        yield return null;

        if (cc != null) cc.enabled = true;

        // Buka kunci pergerakan player
        UnlockPlayerMovement();

        isSitting = false;
        isTransitioning = false;

        // Update UI
        if (standHintPanel != null) standHintPanel.SetActive(false);

        // Delay singkat agar tidak langsung ter-trigger duduk kembali
        yield return new WaitForSeconds(0.5f);
    }

    private void UpdateCanvasPosition()
    {
        if (chairCanvas == null || mainCamera == null) return;

        // Selalu posisikan canvas di depan kamera (0.8m) agar mudah dibaca di VR
        chairCanvas.transform.position = mainCamera.transform.position + mainCamera.transform.forward * canvasDistance;
        chairCanvas.transform.rotation = Quaternion.LookRotation(chairCanvas.transform.position - mainCamera.transform.position);
    }

    // ── Build UI WorldSpace ──
    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("ChairCanvas_" + chairName);
        chairCanvas = canvasObj.AddComponent<Canvas>();
        chairCanvas.renderMode = RenderMode.WorldSpace;
        canvasObj.transform.SetParent(transform, false);

        float scale = canvasScale;
        canvasObj.transform.localScale = new Vector3(scale, scale, scale);
        chairCanvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 1. Panel Hint Duduk
        sitHintPanel = CreateUIBox(canvasObj.transform, "SitHintPanel", new Vector2(520, 65), panelColor);
        sitHintText = CreateText(sitHintPanel.transform, "SitText", $"🪑 Tekan 'A' / Tap untuk Duduk ({chairName})", 22, Color.white);
        sitHintPanel.SetActive(false);

        // 2. Panel Hint Berdiri (muncul saat sedang duduk)
        standHintPanel = CreateUIBox(canvasObj.transform, "StandHintPanel", new Vector2(560, 70), panelColor);
        standHintText = CreateText(standHintPanel.transform, "StandText", "🪑 Anda sedang duduk\nTekan 'B' / Spasi / Klik untuk Berdiri", 20, new Color(0.9f, 0.95f, 1f));
        standHintPanel.SetActive(false);
    }

    private GameObject CreateUIBox(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return obj;
    }

    private Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10, 5);
        rect.offsetMax = new Vector2(-10, -5);

        return text;
    }
}
