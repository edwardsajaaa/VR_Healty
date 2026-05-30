using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script interaksi poster untuk HP Android (VR Cardboard).
///
/// === MASALAH VR ===
/// Canvas "Screen Space - Overlay" TIDAK BISA tampil di Cardboard VR!
/// Script ini membuat World Space Canvas sendiri supaya poster tampil
/// di KEDUA MATA (stereoscopic) dengan benar.
///
/// === CARA KERJA ===
/// 1. Player arahkan cursor ke poster → terdeteksi
/// 2. Tap layar → poster tampil di depan kamera (World Space)
/// 3. Tap layar lagi / tombol tutup → poster tertutup
///
/// === SETUP ===
/// 1. Pasang script ini pada GameObject poster (3D object di scene)
/// 2. Pastikan poster punya Collider (Box Collider)
/// 3. Assign posterImage di Inspector → sprite/gambar yang ingin ditampilkan
/// 4. TIDAK perlu setup Canvas sendiri — script ini buat otomatis
/// </summary>
public class InteractionPoster : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Poster Settings")]
    [SerializeField] private string posterTitle = "Poster";

    [Header("Gambar Poster")]
    [Tooltip("Sprite gambar poster yang akan ditampilkan saat di-klik")]
    [SerializeField] private Sprite posterImage;

    [Header("Panel UI (Opsional)")]
    [Tooltip("Jika sudah punya panel sendiri di Canvas World Space, assign di sini. Jika kosong, script akan membuat otomatis.")]
    [SerializeField] private GameObject existingPanel;

    [Header("Pengaturan Tampilan")]
    [Tooltip("Jarak panel dari kamera (meter)")]
    [SerializeField] private float panelDistance = 2.5f;

    [Tooltip("Lebar panel (meter di World Space)")]
    [SerializeField] private float panelWidth = 1.2f;

    [Tooltip("Tinggi panel (meter di World Space)")]
    [SerializeField] private float panelHeight = 1.6f;

    [Tooltip("Jarak auto-close jika player menjauh dari poster")]
    [SerializeField] private float autoCloseDistance = 10f;

    [Header("Transisi")]
    [SerializeField] private float transitionDuration = 0.3f;

    // ─── State ────────────────────────────────────────────────────────
    private VRWalkController playerController;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isPosterOpen = false;
    private bool isTransitioning = false;
    private bool isGazing = false;

    // ─── VR Canvas (dibuat otomatis) ──────────────────────────────────
    private GameObject vrCanvasObj;
    private Canvas vrCanvas;
    private CanvasGroup vrCanvasGroup;
    private GameObject vrPanelObj;
    private Image vrPosterImageComp;
    private Button vrCloseButton;
    private Coroutine transitionCoroutine;

    [Header("Debug")]
    [SerializeField] private bool dbg_isGazing = false;

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
            gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("[InteractionPoster] " + posterTitle + " auto-tambah BoxCollider.");
        }

        // Pastikan kamera punya PhysicsRaycaster (untuk IPointerClickHandler)
        if (mainCamera != null && mainCamera.GetComponent<PhysicsRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
            Debug.Log("[InteractionPoster] Auto-tambah PhysicsRaycaster ke Main Camera.");
        }

        // Buat VR Canvas
        CreateVRCanvas();
    }

    void Update()
    {
        dbg_isGazing = isGazing;

        // Auto-close jika terlalu jauh
        if (isPosterOpen && playerTransform != null && !isTransitioning)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > autoCloseDistance)
                ClosePoster();
        }

        // ── Fallback: buka via manual raycast + tap ──
        if (!isPosterOpen && !isTransitioning && CheckGazeFallback() && DetectTapFallback())
        {
            Debug.Log("[InteractionPoster] Fallback tap → buka: " + posterTitle);
            OpenPoster();
        }

        // ── Tap lagi saat poster terbuka → tutup ──
        if (isPosterOpen && !isTransitioning && DetectTapFallback())
        {
            // Cek apakah masih melihat poster (jangan tutup kalau melihat tempat lain)
            // Tap saat poster open = tutup poster
            Debug.Log("[InteractionPoster] Tap saat open → tutup: " + posterTitle);
            ClosePoster();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  VR WORLD SPACE CANVAS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Membuat World Space Canvas untuk menampilkan poster di VR.
    /// World Space Canvas tampil di KEDUA MATA (stereoscopic).
    /// Screen Space Overlay TIDAK BISA tampil di Cardboard VR.
    /// </summary>
    private void CreateVRCanvas()
    {
        // ── Jika sudah punya panel existing (World Space), gunakan itu ──
        if (existingPanel != null)
        {
            vrPanelObj = existingPanel;
            vrCanvasGroup = existingPanel.GetComponent<CanvasGroup>();
            if (vrCanvasGroup == null)
                vrCanvasGroup = existingPanel.AddComponent<CanvasGroup>();
            vrCanvasGroup.alpha = 0f;
            existingPanel.SetActive(false);
            return;
        }

        // ── Buat Canvas World Space baru ──
        vrCanvasObj = new GameObject("VRPosterCanvas_" + posterTitle);
        vrCanvasObj.transform.SetParent(null); // root level, bukan child kamera

        vrCanvas = vrCanvasObj.AddComponent<Canvas>();
        vrCanvas.renderMode = RenderMode.WorldSpace;
        vrCanvas.sortingOrder = 100;

        // Ukuran canvas di world space
        RectTransform canvasRect = vrCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 1400); // pixel size internal
        canvasRect.localScale = new Vector3(panelWidth / 1000f, panelHeight / 1400f, 1f);

        vrCanvasObj.AddComponent<GraphicRaycaster>();

        vrCanvasGroup = vrCanvasObj.AddComponent<CanvasGroup>();
        vrCanvasGroup.alpha = 0f;

        // ── Background panel (gelap transparan) ──
        vrPanelObj = new GameObject("PosterPanel");
        vrPanelObj.transform.SetParent(vrCanvasObj.transform, false);
        Image bgImage = vrPanelObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
        RectTransform bgRect = vrPanelObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ── Gambar poster ──
        if (posterImage != null)
        {
            GameObject imgObj = new GameObject("PosterImage");
            imgObj.transform.SetParent(vrPanelObj.transform, false);
            vrPosterImageComp = imgObj.AddComponent<Image>();
            vrPosterImageComp.sprite = posterImage;
            vrPosterImageComp.preserveAspect = true;

            RectTransform imgRect = imgObj.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.03f, 0.08f);
            imgRect.anchorMax = new Vector2(0.97f, 0.92f);
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;
        }
        else
        {
            // Tampilkan teks jika tidak ada gambar
            GameObject txtObj = new GameObject("NoPosterText");
            txtObj.transform.SetParent(vrPanelObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = posterTitle + "\n\n(Tidak ada gambar)";
            txt.fontSize = 48;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(30, 80);
            txtRect.offsetMax = new Vector2(-30, -30);
        }

        // ── Judul ──
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(vrPanelObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = posterTitle;
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.93f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, 0);

        // ── Tombol Tutup (di bawah panel) ──
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(vrPanelObj.transform, false);

        Image closeBtnImg = closeBtnObj.AddComponent<Image>();
        closeBtnImg.color = new Color(0.85f, 0.2f, 0.2f, 0.95f);

        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.25f, 0);
        closeBtnRect.anchorMax = new Vector2(0.75f, 0);
        closeBtnRect.pivot = new Vector2(0.5f, 1);
        closeBtnRect.sizeDelta = new Vector2(0, 80);
        closeBtnRect.anchoredPosition = new Vector2(0, -5);

        vrCloseButton = closeBtnObj.AddComponent<Button>();
        ColorBlock cb = vrCloseButton.colors;
        cb.normalColor = new Color(0.85f, 0.2f, 0.2f, 0.95f);
        cb.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        cb.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
        vrCloseButton.colors = cb;
        vrCloseButton.targetGraphic = closeBtnImg;
        vrCloseButton.onClick.AddListener(() => { ClosePoster(); });

        // Tambah collider ke close button supaya bisa di-klik via reticle
        BoxCollider closeBtnCol = closeBtnObj.AddComponent<BoxCollider>();
        closeBtnCol.size = new Vector3(500, 80, 1);

        GameObject closeTextObj = new GameObject("CloseLabel");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        Text closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "✕ TUTUP";
        closeText.fontSize = 38;
        closeText.fontStyle = FontStyle.Bold;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        // Sembunyikan canvas
        vrCanvasObj.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CARDBOARD POINTER EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[InteractionPoster] OnPointerClick: " + posterTitle);
        if (!isPosterOpen && !isTransitioning)
            OpenPoster();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isGazing = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isGazing = false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FALLBACK DETECTION
    // ═══════════════════════════════════════════════════════════════════

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

    private bool DetectTapFallback()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;
        if (Input.GetMouseButtonDown(0))
            return true;
        if (Input.GetButtonDown("Fire1"))
            return true;
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  OPEN / CLOSE
    // ═══════════════════════════════════════════════════════════════════

    private void OpenPoster()
    {
        Debug.Log("[InteractionPoster] MEMBUKA: " + posterTitle);

        isPosterOpen = true;

        if (playerController != null)
            playerController.LockMovement();

        // Posisikan canvas di depan kamera
        PositionCanvasInFrontOfCamera();

        // Aktifkan dan animasi
        if (vrCanvasObj != null) vrCanvasObj.SetActive(true);
        if (existingPanel != null) existingPanel.SetActive(true);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionOpen());
    }

    public void ClosePoster()
    {
        Debug.Log("[InteractionPoster] MENUTUP: " + posterTitle);

        if (playerController != null)
            playerController.UnlockMovement();

        isPosterOpen = false;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionClose());
    }

    /// <summary>
    /// Posisikan World Space Canvas di depan kamera player.
    /// Canvas menghadap ke arah kamera supaya mudah dibaca.
    /// </summary>
    private void PositionCanvasInFrontOfCamera()
    {
        Transform target = vrCanvasObj != null ? vrCanvasObj.transform :
                           existingPanel != null ? existingPanel.transform : null;
        if (target == null || mainCamera == null) return;

        // Posisi: di depan kamera, sejajar pandangan
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0; // Jaga tetap tegak (jangan ikut tilt kepala)
        if (forward.sqrMagnitude < 0.01f)
            forward = mainCamera.transform.forward; // Fallback jika melihat langsung ke atas/bawah

        forward.Normalize();

        Vector3 position = mainCamera.transform.position + forward * panelDistance;
        position.y = mainCamera.transform.position.y; // Sejajar mata

        target.position = position;
        target.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ANIMASI TRANSISI
    // ═══════════════════════════════════════════════════════════════════

    private IEnumerator TransitionOpen()
    {
        isTransitioning = true;
        float elapsed = 0f;

        if (vrCanvasGroup != null) vrCanvasGroup.alpha = 0f;

        Transform panelT = vrPanelObj != null ? vrPanelObj.transform :
                           existingPanel != null ? existingPanel.transform : null;
        if (panelT != null) panelT.localScale = new Vector3(0.85f, 0.85f, 1f);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float eased = EaseOutBack(t);

            if (vrCanvasGroup != null)
                vrCanvasGroup.alpha = Mathf.Clamp01(t / 0.6f);

            if (panelT != null)
            {
                float scale = Mathf.LerpUnclamped(0.85f, 1f, eased);
                panelT.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        if (vrCanvasGroup != null) vrCanvasGroup.alpha = 1f;
        if (panelT != null) panelT.localScale = Vector3.one;
        isTransitioning = false;
    }

    private IEnumerator TransitionClose()
    {
        isTransitioning = true;
        float elapsed = 0f;

        float startAlpha = vrCanvasGroup != null ? vrCanvasGroup.alpha : 1f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (vrCanvasGroup != null)
                vrCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        if (vrCanvasGroup != null) vrCanvasGroup.alpha = 0f;
        if (vrCanvasObj != null) vrCanvasObj.SetActive(false);
        if (existingPanel != null) existingPanel.SetActive(false);
        isTransitioning = false;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═══════════════════════════════════════════════════════════════════

    void OnDestroy()
    {
        if (vrCanvasObj != null)
            Destroy(vrCanvasObj);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoCloseDistance);
    }
}
