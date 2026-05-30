using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script interaksi poster untuk HP Android (VR Cardboard).
/// Semua interaksi berbasis CURSOR (gaze) + TAP layar.
///
/// === CARA KERJA ===
/// 1. Player mengarahkan cursor ke poster → terdeteksi gaze
/// 2. Player tap layar saat melihat poster → panel poster terbuka
/// 3. Player tap tombol "✕ Tutup" → panel poster tertutup
/// 4. Otomatis tutup jika player pergi terlalu jauh
///
/// === SETUP ===
/// 1. Pasang script ini pada GameObject poster
/// 2. Pastikan poster punya Collider (Box Collider)
/// 3. Assign posterPanel di Inspector (panel UI yang muncul)
/// 4. Tombol tutup otomatis dibuat, atau assign manual di Inspector
/// </summary>
public class InteractionPoster : MonoBehaviour
{
    [Header("Poster Settings")]
    [SerializeField] private string posterTitle = "Poster";

    [Header("Panel UI")]
    [SerializeField] private GameObject posterPanel;

    [Header("Tombol Tutup (Opsional)")]
    [Tooltip("Jika dikosongkan, tombol tutup akan dibuat otomatis di bawah panel.")]
    [SerializeField] private Button closeButton;

    [Header("Interaksi Gaze")]
    [Tooltip("Jarak maksimal cursor bisa mendeteksi poster")]
    [SerializeField] private float gazeDistance = 5f;

    [Tooltip("Jarak maksimal sebelum poster auto-tutup")]
    [SerializeField] private float autoCloseDistance = 6f;

    [Header("Transisi")]
    [SerializeField] private float transitionDuration = 0.35f;

    // ─── State ────────────────────────────────────────────────────────
    private VRWalkController playerController;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isPosterOpen = false;
    private bool isTransitioning = false;
    private bool isGazingAtPoster = false;

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

        // Pastikan poster punya collider
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("[InteractionPoster] " + posterTitle + 
                " tidak punya Collider, auto-tambah BoxCollider. Atur ukurannya manual di Inspector.");
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
        if (mainCamera == null) { mainCamera = Camera.main; return; }

        // ── Cek gaze (cursor mengarah ke poster) ──
        isGazingAtPoster = CheckGaze();
        dbg_isGazing = isGazingAtPoster;

        if (playerTransform != null)
            dbg_distance = Vector3.Distance(transform.position, playerTransform.position);

        // ── Buka poster: gaze + tap layar ──
        if (isGazingAtPoster && !isPosterOpen && !isTransitioning)
        {
            if (DetectTap())
                OpenPoster();
        }

        // ── Auto-tutup jika terlalu jauh ──
        if (isPosterOpen && playerTransform != null && !isTransitioning)
        {
            if (dbg_distance > autoCloseDistance)
                ClosePoster();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GAZE DETECTION (CURSOR)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cek apakah cursor (tengah layar / gaze) mengarah ke poster ini.
    /// Menggunakan raycast dari kamera ke depan.
    /// </summary>
    private bool CheckGaze()
    {
        if (mainCamera == null) return false;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TAP DETECTION (ANDROID + EDITOR)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deteksi tap layar. TIDAK pakai IsPointerOverGameObject
    /// karena bermasalah di Cardboard Android.
    /// Filtering dilakukan via gaze check — jika player sudah melihat poster,
    /// maka tap = buka poster.
    /// </summary>
    private bool DetectTap()
    {
        // Android touch
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        // Editor fallback: klik kiri mouse
        if (Input.GetMouseButtonDown(0))
            return true;

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  OPEN / CLOSE
    // ═══════════════════════════════════════════════════════════════════

    private void OpenPoster()
    {
        if (posterPanel == null || canvasGroup == null) return;

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
        // Selalu unlock movement dulu, apapun yang terjadi (fix bug terkunci)
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

    /// <summary>
    /// Setup tombol tutup. Jika sudah di-assign di Inspector, pakai itu.
    /// Jika tidak, buat otomatis di bawah panel.
    /// </summary>
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
    //  TRIGGER EVENTS (BACKUP)
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gazeDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoCloseDistance);
    }
}
