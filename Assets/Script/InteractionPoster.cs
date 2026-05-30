using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script interaksi poster untuk HP Android (VR Cardboard).
/// Menggunakan IPointerClickHandler agar kompatibel dengan CardboardReticlePointer.
///
/// === CARA KERJA ===
/// 1. Player arahkan cursor (reticle) ke poster
/// 2. Tap layar (Cardboard trigger) → panel poster terbuka
/// 3. Tap tombol "✕ Tutup" → panel poster tertutup
///
/// === SETUP ===
/// 1. Pasang script ini pada GameObject poster
/// 2. Pastikan poster punya Collider (Box Collider) — JANGAN centang Is Trigger
/// 3. Pastikan Main Camera punya komponen PhysicsRaycaster
///    (script ini auto-tambah jika belum ada)
/// 4. Assign posterPanel di Inspector (panel UI yang muncul)
/// </summary>
public class InteractionPoster : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Poster Settings")]
    [SerializeField] private string posterTitle = "Poster";

    [Header("Panel UI")]
    [SerializeField] private GameObject posterPanel;

    [Header("Tombol Tutup (Opsional)")]
    [Tooltip("Jika dikosongkan, tombol tutup akan dibuat otomatis di bawah panel.")]
    [SerializeField] private Button closeButton;

    [Header("Interaksi")]
    [Tooltip("Jarak auto-close jika player menjauh")]
    [SerializeField] private float autoCloseDistance = 8f;

    [Header("Transisi")]
    [SerializeField] private float transitionDuration = 0.35f;

    // ─── State ────────────────────────────────────────────────────────
    private VRWalkController playerController;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isPosterOpen = false;
    private bool isTransitioning = false;
    private bool isGazing = false;

    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Coroutine transitionCoroutine;

    [Header("Debug (Read Only)")]
    [SerializeField] private bool dbg_isGazing = false;
    [SerializeField] private float dbg_distance = 0f;

    // ═══════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════

    void Start()
    {
        mainCamera = Camera.main;
        playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null)
            playerTransform = playerController.transform;

        // Pastikan poster punya collider untuk raycast
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("[InteractionPoster] " + posterTitle +
                " tidak punya Collider, auto-tambah BoxCollider.");
        }

        // ══ PENTING: Pastikan kamera punya PhysicsRaycaster ══
        // Tanpa ini, IPointerClickHandler TIDAK BISA menerima event dari
        // Cardboard reticle pointer pada object 3D.
        if (mainCamera != null)
        {
            PhysicsRaycaster pr = mainCamera.GetComponent<PhysicsRaycaster>();
            if (pr == null)
            {
                pr = mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
                Debug.Log("[InteractionPoster] Auto-tambah PhysicsRaycaster ke Main Camera.");
            }
        }

        if (posterPanel != null)
        {
            canvasGroup = posterPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = posterPanel.AddComponent<CanvasGroup>();

            panelRect = posterPanel.GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            if (panelRect != null) panelRect.localScale = Vector3.zero;
            posterPanel.SetActive(false);

            SetupCloseButton();
        }
    }

    void Update()
    {
        dbg_isGazing = isGazing;

        if (playerTransform != null)
            dbg_distance = Vector3.Distance(transform.position, playerTransform.position);

        // ── Auto-tutup jika terlalu jauh ──
        if (isPosterOpen && playerTransform != null && !isTransitioning)
        {
            if (dbg_distance > autoCloseDistance)
                ClosePoster();
        }

        // ── Fallback: buka poster dengan tap manual (jaga-jaga IPointer tidak jalan) ──
        if (!isPosterOpen && !isTransitioning && CheckGazeFallback() && DetectTapFallback())
        {
            Debug.Log("[InteractionPoster] Fallback tap detected untuk: " + posterTitle);
            OpenPoster();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CARDBOARD POINTER EVENTS (CARA UTAMA)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dipanggil oleh Cardboard EventSystem saat player tap layar
    /// sambil reticle mengarah ke poster ini.
    /// INI ADALAH CARA UTAMA INTERAKSI.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[InteractionPoster] OnPointerClick pada: " + posterTitle);

        if (!isPosterOpen && !isTransitioning)
            OpenPoster();
    }

    /// <summary>
    /// Dipanggil saat reticle/cursor masuk ke area poster.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isGazing = true;
        Debug.Log("[InteractionPoster] Gaze MASUK ke: " + posterTitle);
    }

    /// <summary>
    /// Dipanggil saat reticle/cursor keluar dari area poster.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isGazing = false;
        Debug.Log("[InteractionPoster] Gaze KELUAR dari: " + posterTitle);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FALLBACK DETECTION (KALAU EVENTSYSTEM TIDAK JALAN)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fallback gaze check manual pakai raycast, jaga-jaga EventSystem tidak bekerja.
    /// </summary>
    private bool CheckGazeFallback()
    {
        if (mainCamera == null) return false;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Fallback tap detection — cek SEMUA jenis input yang mungkin.
    /// </summary>
    private bool DetectTapFallback()
    {
        // Touch (Android)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        // Mouse click (Editor + Cardboard trigger yang di-map ke mouse)
        if (Input.GetMouseButtonDown(0))
            return true;

        // Cardboard trigger button (beberapa versi SDK)
        if (Input.GetButtonDown("Fire1"))
            return true;

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  OPEN / CLOSE
    // ═══════════════════════════════════════════════════════════════════

    private void OpenPoster()
    {
        if (posterPanel == null || canvasGroup == null)
        {
            Debug.LogError("[InteractionPoster] posterPanel atau canvasGroup NULL pada: " + posterTitle);
            return;
        }

        Debug.Log("[InteractionPoster] MEMBUKA poster: " + posterTitle);
        isPosterOpen = true;

        if (playerController != null)
            playerController.LockMovement();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        posterPanel.SetActive(true);
        transitionCoroutine = StartCoroutine(TransitionOpen());
    }

    public void ClosePoster()
    {
        Debug.Log("[InteractionPoster] MENUTUP poster: " + posterTitle);

        // Selalu unlock movement dulu (fix bug terkunci)
        if (playerController != null)
            playerController.UnlockMovement();

        isPosterOpen = false;

        if (posterPanel == null || canvasGroup == null) return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionClose());
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TOMBOL TUTUP
    // ═══════════════════════════════════════════════════════════════════

    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { ClosePoster(); });
            return;
        }

        // ── Buat tombol tutup otomatis di BAWAH panel ──
        GameObject btnObj = new GameObject("CloseButton_Auto");
        btnObj.transform.SetParent(posterPanel.transform, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.2f, 0.2f, 0.95f);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0);
        btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 1);
        btnRect.sizeDelta = new Vector2(180, 55);
        btnRect.anchoredPosition = new Vector2(0, -15);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.85f, 0.2f, 0.2f, 0.95f);
        cb.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        cb.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
        btn.colors = cb;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => { ClosePoster(); });

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "✕ Tutup";
        btnText.fontSize = 22;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        closeButton = btn;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ANIMASI TRANSISI
    // ═══════════════════════════════════════════════════════════════════

    private IEnumerator TransitionOpen()
    {
        isTransitioning = true;
        float elapsed = 0f;

        canvasGroup.alpha = 0f;
        if (panelRect != null) panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float eased = EaseOutBack(t);

            canvasGroup.alpha = Mathf.Clamp01(t / 0.6f);

            if (panelRect != null)
            {
                float scale = Mathf.LerpUnclamped(0.85f, 1f, eased);
                panelRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (panelRect != null) panelRect.localScale = Vector3.one;
        isTransitioning = false;
    }

    private IEnumerator TransitionClose()
    {
        isTransitioning = true;
        float elapsed = 0f;

        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = panelRect != null ? panelRect.localScale : Vector3.one;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float eased = EaseInCubic(t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            if (panelRect != null)
            {
                float scale = Mathf.Lerp(startScale.x, 0.85f, eased);
                panelRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (panelRect != null) panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);
        posterPanel.SetActive(false);
        isTransitioning = false;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRIGGER EVENTS (BACKUP AUTO-CLOSE)
    // ═══════════════════════════════════════════════════════════════════

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") ||
            other.GetComponent<VRWalkController>() != null ||
            other.GetComponentInParent<VRWalkController>() != null)
        {
            if (isPosterOpen) ClosePoster();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoCloseDistance);
    }
}
