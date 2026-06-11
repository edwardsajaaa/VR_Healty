using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class InteractionPoster : MonoBehaviour
{
    [Header("Poster Settings")]
    [SerializeField] private Sprite[] posterImages;
    [SerializeField] private string posterTitle = "Poster Kesehatan";

    [Header("3D Floating Title (Opsional)")]
    [Tooltip("Drag & drop objek teks (Text/Canvas/TextMeshPro) dari Scene ke sini")]
    [SerializeField] private GameObject floatingTitleObject;

    [Header("Interaksi")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float canvasDistance = 0.8f;
    [SerializeField] private float canvasScale = 0.001f;

    [Header("Custom UI Sprites")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;

    [Header("Warna UI (Dark Theme)")]
    [SerializeField] private Color panelColor = new Color(0.11f, 0.13f, 0.17f, 0.96f);
    [SerializeField] private Color headerColor = new Color(0.07f, 0.09f, 0.13f, 1.0f);
    [SerializeField] private Color accentColor = new Color(0.35f, 0.55f, 0.95f, 1.0f);
    [SerializeField] private Color headerTextColor = Color.white;
    [SerializeField] private Color hintTextColor = new Color(0.85f, 0.90f, 0.95f, 1.0f);
    [SerializeField] private Color hintBgColor = new Color(0.10f, 0.12f, 0.16f, 0.88f);
    [SerializeField] private Color cardColor = new Color(0.16f, 0.18f, 0.23f, 1.0f);
    [SerializeField] private Color cardPressColor = new Color(0.22f, 0.25f, 0.32f, 1.0f);
    [SerializeField] private Color closeBtnColor = new Color(0.75f, 0.22f, 0.22f, 1.0f);
    [SerializeField] private Color backBtnColor = new Color(0.22f, 0.25f, 0.32f, 1.0f);

    // ─── State ───
    private bool isPlayerNearby = false;
    private bool isPosterOpen = false;
    private bool isDetailView = false;
    private bool isClosing = false;

    // Flag STATIS: berlaku untuk SEMUA instance InteractionPoster.
    // Memastikan hanya 1 poster bisa terbuka sekaligus, dan tap UI
    // tidak membuka poster lain di scene yang sama.
    private static bool anyPosterIsOpen = false;

    private Camera mainCamera;
    private VRWalkController playerController;

    // ─── UI References ───
    private Canvas posterCanvas;
    private CanvasGroup canvasGroup;
    private GameObject hintPanel;
    private GameObject posterPanel;
    private GameObject galleryView;
    private GameObject detailView;
    private Image detailImage;
    private GameObject backButton;
    private Coroutine fadeCoroutine;

    void Start()
    {
        mainCamera = Camera.main;
        playerController = FindObjectOfType<VRWalkController>();
        BuildUI();

        // Jika user memasukkan objek teks, otomatis cari komponen teks apa pun
        // (UI Text, TextMeshPro, TextMesh) dan set isinya
        if (floatingTitleObject != null)
        {
            UnityEngine.UI.Text uiText = floatingTitleObject.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) uiText.text = posterTitle;

            TMPro.TMP_Text tmpText = floatingTitleObject.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmpText != null) tmpText.text = posterTitle;

            TextMesh legacyTm = floatingTitleObject.GetComponentInChildren<TextMesh>();
            if (legacyTm != null) legacyTm.text = posterTitle;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        bool nearby = CheckPlayerNearby();

        if (nearby && !isPlayerNearby && !isPosterOpen)
        {
            isPlayerNearby = true;
            if (hintPanel != null) hintPanel.SetActive(true);
        }
        else if (!nearby && isPlayerNearby && !isPosterOpen)
        {
            isPlayerNearby = false;
            if (hintPanel != null) hintPanel.SetActive(false);
        }

        // Auto-tutup jika player pergi jauh saat poster terbuka
        if (!nearby && isPosterOpen)
        {
            ClosePoster();
        }

        // Tap untuk menutup poster — HANYA di Gallery View.
        // Di Detail View, player harus gunakan tombol "Kembali" atau "Tutup".
        // Buka poster dengan tombol controller saat player dekat.
        if (isPosterOpen && !isDetailView && !isClosing && DetectControllerInteract())
        {
            ClosePoster();
        }

        // Buka poster dengan tombol controller saat player dekat.
        // Guard: tidak membuka jika sedang menutup ATAU ada poster lain sedang terbuka.
        if (isPlayerNearby && !isPosterOpen && !isClosing && !anyPosterIsOpen && DetectControllerInteract())
            OpenPoster();
    }

    /// <summary>
    /// Mendeteksi penekanan tombol pada Controller VR.
    /// Tombol 'C' pada remote VR murah biasanya dipetakan ke JoystickButton2.
    /// </summary>
    private bool DetectControllerInteract()
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

        // Fallback Keyboard (tekan C)
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            return true;
        }

        // Editor / PC fallback: klik kiri mouse di luar UI
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        // Android Touch fallback: tap di luar UI
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            int fingerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId))
                return false;
            return true;
        }

        return false;
    }

    private bool CheckPlayerNearby()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        return distance <= interactionDistance;
    }

    // ─────────────────────────────────────────────
    //  POSTER STATE MANAGEMENT
    // ─────────────────────────────────────────────

    private void OpenPoster()
    {
        isPosterOpen = true;
        anyPosterIsOpen = true; // Beritahu semua instance: ada poster terbuka
        if (hintPanel != null) hintPanel.SetActive(false);

        // Aktifkan navigasi controller di EventSystem
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = true;
        }

        // Sembunyikan floating title saat poster dibuka
        if (floatingTitleObject != null) floatingTitleObject.SetActive(false);

        // Jika hanya 1 gambar → langsung ke Detail View (skip gallery)
        int imgCount = (posterImages != null) ? posterImages.Length : 0;
        if (imgCount <= 1)
        {
            isDetailView = true;
            if (imgCount == 1)
                detailImage.sprite = posterImages[0];

            galleryView.SetActive(false);
            detailView.SetActive(true);
            if (backButton != null) backButton.SetActive(false);
        }
        else
        {
            ShowGalleryView();
        }

        posterPanel.SetActive(true);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f));

        if (playerController != null) playerController.LockMovement();
    }

    private void ShowGalleryView()
    {
        isDetailView = false;
        galleryView.SetActive(true);
        detailView.SetActive(false);
        if (backButton != null) backButton.SetActive(false);

        // Pilih item pertama secara otomatis agar bisa discroll dengan analog
        StartCoroutine(SelectFirstCardDelay());
    }

    private IEnumerator SelectFirstCardDelay()
    {
        yield return null; // Tunggu 1 frame sampai UI aktif
        if (galleryView != null && galleryView.activeInHierarchy)
        {
            Transform content = galleryView.GetComponentInChildren<ScrollRect>().content;
            if (content.childCount > 0)
            {
                Button firstBtn = content.GetChild(0).GetComponent<Button>();
                if (firstBtn != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstBtn.gameObject);
                }
            }
        }
    }

    private void ShowDetailView(int index)
    {
        if (posterImages == null || index < 0 || index >= posterImages.Length) return;

        isDetailView = true;
        detailImage.sprite = posterImages[index];
        galleryView.SetActive(false);
        detailView.SetActive(true);

        // Tampilkan tombol "Kembali" hanya jika ada lebih dari 1 poster
        if (backButton != null)
        {
            backButton.SetActive(posterImages.Length > 1);
            if (EventSystem.current != null && backButton.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(backButton);
            }
        }
    }

    private void ClosePoster()
    {
        if (isClosing) return; // Cegah double-call

        isPosterOpen = false;
        isPlayerNearby = false;
        isDetailView = false;
        isClosing = true;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, () =>
        {
            posterPanel.SetActive(false);

            // Reset ke gallery view untuk next open
            galleryView.SetActive(true);
            detailView.SetActive(false);
            if (backButton != null) backButton.SetActive(false);

            anyPosterIsOpen = false; // Bebaskan flag global

            // Munculkan kembali floating title setelah poster ditutup
            if (floatingTitleObject != null) floatingTitleObject.SetActive(true);

            // Delay kecil agar tap yang sama tidak langsung re-open
            StartCoroutine(ResetClosingFlag());
        }));

        if (playerController != null) playerController.UnlockMovement();
    }

    private IEnumerator ResetClosingFlag()
    {
        // Tunggu 2 frame agar input tap sudah berlalu
        yield return null;
        yield return null;
        isClosing = false;
    }

    private IEnumerator FadeCanvas(float from, float to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    // ═════════════════════════════════════════════
    //  UI BUILDING — Profesional Dark Theme
    // ═════════════════════════════════════════════

    private void BuildUI()
    {
        // Pastikan EventSystem ada
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // ── Canvas (WorldSpace, dekat player) ──
        GameObject canvasObj = new GameObject("PosterCanvas_" + posterTitle);
        posterCanvas = canvasObj.AddComponent<Canvas>();

        // WorldSpace agar berfungsi di VR Cardboard
        posterCanvas.renderMode = RenderMode.WorldSpace;
        canvasObj.transform.SetParent(mainCamera.transform, false);

        // Posisikan canvas lebih dekat ke kamera (0.8m default)
        canvasObj.transform.localPosition = new Vector3(0, 0, canvasDistance);
        canvasObj.transform.localRotation = Quaternion.identity;

        // Skala lebih besar agar UI nyaman dibaca
        canvasObj.transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);

        posterCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Build Sub-Panels ──
        BuildHintPanel(canvasObj.transform);
        BuildPosterPanel(canvasObj.transform);
    }

    // ─── Hint Panel ("Ketuk layar untuk melihat poster") ───
    private void BuildHintPanel(Transform parent)
    {
        hintPanel = CreatePanel(parent, "HintPanel",
            new Vector2(480, 56),
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f),
            panelSprite, hintBgColor);

        // Bayangan halus pada hint
        Shadow hintShadow = hintPanel.AddComponent<Shadow>();
        hintShadow.effectColor = new Color(0, 0, 0, 0.4f);
        hintShadow.effectDistance = new Vector2(2, -2);

        Text hintText = CreateText(hintPanel.transform, "HintText",
            "\u25C9 Tekan 'C' untuk melihat poster",
            20, TextAnchor.MiddleCenter, hintTextColor);
        StretchRect(hintText.rectTransform, 20, 5, -20, -5);

        hintPanel.SetActive(false);
    }

    // ─── Poster Panel Utama ───
    private void BuildPosterPanel(Transform parent)
    {
        float panelW = 1200f;
        float panelH = 850f;

        posterPanel = CreatePanel(parent, "PosterPanel",
            new Vector2(panelW, panelH),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            panelSprite, panelColor);

        // Bayangan panel utama
        Shadow panelShadow = posterPanel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0, 0, 0, 0.5f);
        panelShadow.effectDistance = new Vector2(4, -4);

        canvasGroup = posterPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        posterPanel.SetActive(false);

        // ── Sub-komponen ──
        BuildHeader(posterPanel.transform);
        BuildGalleryView(posterPanel.transform);
        BuildDetailView(posterPanel.transform);
        BuildBottomClose(posterPanel.transform);
    }

    // ─── Header Bar + Accent Line ───
    private void BuildHeader(Transform parent)
    {
        // Header background
        GameObject headerBar = CreatePanel(parent, "HeaderBar",
            Vector2.zero, Vector2.zero, Vector2.zero,
            buttonSprite, headerColor);
        RectTransform hbRect = headerBar.GetComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0, 1);
        hbRect.anchorMax = new Vector2(1, 1);
        hbRect.pivot = new Vector2(0.5f, 1);
        hbRect.sizeDelta = new Vector2(0, 100);
        hbRect.anchoredPosition = Vector2.zero;

        backButton = CreatePanel(headerBar.transform, "BackBtn",
            new Vector2(300, 75),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            buttonSprite, backBtnColor);
        backButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);

        Button backBtnComp = backButton.AddComponent<Button>();
        Image backBtnImg = backButton.GetComponent<Image>();
        ColorBlock bcb = backBtnComp.colors;
        bcb.normalColor = backBtnColor;
        bcb.highlightedColor = backBtnColor * 1.2f;
        bcb.pressedColor = backBtnColor * 0.8f;
        backBtnComp.colors = bcb;
        backBtnComp.targetGraphic = backBtnImg;
        backBtnComp.onClick.AddListener(() => ShowGalleryView());

        Text backLabel = CreateText(backButton.transform, "BackLabel",
            "\u2190 Kembali", 32, TextAnchor.MiddleCenter, Color.white);
        StretchRect(backLabel.rectTransform, 5, 2, -5, -2);
        backButton.SetActive(false);

        // ─ Judul poster (tengah) ─
        Text titleText = CreateText(headerBar.transform, "TitleText",
            posterTitle, 32, TextAnchor.MiddleCenter, headerTextColor);
        titleText.fontStyle = FontStyle.Bold;
        RectTransform ttRect = titleText.rectTransform;
        ttRect.anchorMin = Vector2.zero;
        ttRect.anchorMax = Vector2.one;
        ttRect.offsetMin = new Vector2(340, 0);
        ttRect.offsetMax = new Vector2(-15, 0);

        // (Tombol Close ✕ pojok kanan dihapus — cukup gunakan tombol besar di bawah)

        // ─ Accent line (garis tipis berwarna di bawah header) ─
        GameObject accentLine = CreatePanel(parent, "AccentLine",
            Vector2.zero, Vector2.zero, Vector2.zero,
            null, accentColor);
        RectTransform alRect = accentLine.GetComponent<RectTransform>();
        alRect.anchorMin = new Vector2(0, 1);
        alRect.anchorMax = new Vector2(1, 1);
        alRect.pivot = new Vector2(0.5f, 1);
        alRect.sizeDelta = new Vector2(0, 3);
        alRect.anchoredPosition = new Vector2(0, -100);
    }

    // ─── Gallery View (ScrollRect + Grid 3 Kolom) ───
    private void BuildGalleryView(Transform parent)
    {
        galleryView = new GameObject("GalleryView");
        galleryView.transform.SetParent(parent, false);
        RectTransform gvRect = galleryView.AddComponent<RectTransform>();
        gvRect.anchorMin = Vector2.zero;
        gvRect.anchorMax = Vector2.one;
        gvRect.offsetMin = new Vector2(10, 58);   // di atas tombol tutup
        gvRect.offsetMax = new Vector2(-10, -108);  // di bawah header + accent

        // ── ScrollRect (scroll vertikal) ──
        ScrollRect scrollRect = galleryView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;

        // ── Viewport (area terlihat dengan Mask) ──
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(galleryView.transform, false);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        // RectMask2D untuk clip konten scroll tanpa masalah alpha
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRect;

        // ── Content (GridLayoutGroup: maks 3 kolom) ──
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRect = content.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 0);
        scrollRect.content = cRect;

        // Grid: 3 kolom, ukuran cell otomatis
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(350f, 490f);
        grid.spacing = new Vector2(20, 20);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.childAlignment = TextAnchor.UpperCenter;

        // ContentSizeFitter agar tinggi content mengikuti jumlah poster
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Buat poster card untuk setiap gambar ──
        if (posterImages != null && posterImages.Length > 0)
        {
            for (int i = 0; i < posterImages.Length; i++)
            {
                CreatePosterCard(content.transform, i);
            }
        }
        else
        {
            // Placeholder jika tidak ada gambar
            GameObject emptyObj = new GameObject("EmptyState");
            emptyObj.transform.SetParent(content.transform, false);
            emptyObj.AddComponent<RectTransform>();
            Image emptyImg = emptyObj.AddComponent<Image>();
            emptyImg.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);

            Text emptyText = CreateText(emptyObj.transform, "EmptyText",
                "Tidak ada poster", 18, TextAnchor.MiddleCenter,
                new Color(0.5f, 0.5f, 0.55f));
            StretchRect(emptyText.rectTransform, 10, 10, -10, -10);
        }
    }

    /// <summary>
    /// Membuat satu card poster di gallery grid.
    /// Setiap card berisi thumbnail gambar dan badge nomor.
    /// </summary>
    private void CreatePosterCard(Transform parent, int index)
    {
        // ── Card container ──
        GameObject card = CreatePanel(parent, "PosterCard_" + index,
            Vector2.zero, Vector2.zero, Vector2.zero,
            buttonSprite, cardColor);

        // Bayangan card
        Shadow cardShadow = card.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0, 0, 0, 0.35f);
        cardShadow.effectDistance = new Vector2(2, -2);

        // ── Button behavior (tap untuk masuk Detail View) ──
        Button cardBtn = card.AddComponent<Button>();
        Image cardBgImg = card.GetComponent<Image>();
        ColorBlock cb = cardBtn.colors;
        cb.normalColor = cardColor;
        cb.highlightedColor = cardPressColor;
        cb.pressedColor = new Color(
            accentColor.r * 0.5f,
            accentColor.g * 0.5f,
            accentColor.b * 0.5f, 0.8f);
        cb.selectedColor = cardColor;
        cardBtn.colors = cb;
        cardBtn.targetGraphic = cardBgImg;

        int capturedIndex = index;
        cardBtn.onClick.AddListener(() => ShowDetailView(capturedIndex));

        // ── Poster thumbnail image ──
        GameObject imgObj = new GameObject("Thumbnail");
        imgObj.transform.SetParent(card.transform, false);
        Image thumbImg = imgObj.AddComponent<Image>();
        thumbImg.sprite = posterImages[index];
        thumbImg.preserveAspect = true;
        thumbImg.raycastTarget = false; // Agar klik tembus ke card button
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = new Vector2(6, 28);  // Ruang untuk badge di bawah
        imgRect.offsetMax = new Vector2(-6, -6);

        // ── Number badge (pojok kiri bawah) ──
        GameObject badge = CreatePanel(card.transform, "Badge",
            new Vector2(40, 30),
            new Vector2(0, 0), new Vector2(0, 0),
            null, accentColor);
        badge.GetComponent<RectTransform>().anchoredPosition = new Vector2(6, 6);
        badge.GetComponent<Image>().raycastTarget = false;

        Text badgeText = CreateText(badge.transform, "BadgeNum",
            (index + 1).ToString(),
            16, TextAnchor.MiddleCenter, Color.white);
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.raycastTarget = false;
        StretchRect(badgeText.rectTransform, 0, 0, 0, 0);
    }

    // ─── Detail View (Poster Fullscreen) ───
    private void BuildDetailView(Transform parent)
    {
        detailView = new GameObject("DetailView");
        detailView.transform.SetParent(parent, false);
        RectTransform dvRect = detailView.AddComponent<RectTransform>();
        dvRect.anchorMin = Vector2.zero;
        dvRect.anchorMax = Vector2.one;
        dvRect.offsetMin = new Vector2(15, 58);
        dvRect.offsetMax = new Vector2(-15, -108);

        // Background area detail (sedikit lebih gelap)
        Image dvBg = detailView.AddComponent<Image>();
        dvBg.color = new Color(0.08f, 0.09f, 0.12f, 0.5f);
        dvBg.raycastTarget = false;

        // Gambar poster detail (besar, preserve aspect)
        GameObject imgObj = new GameObject("DetailImage");
        imgObj.transform.SetParent(detailView.transform, false);
        detailImage = imgObj.AddComponent<Image>();
        detailImage.preserveAspect = true;
        detailImage.raycastTarget = false;
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = new Vector2(8, 8);
        imgRect.offsetMax = new Vector2(-8, -8);

        detailView.SetActive(false);
    }

    // ─── Tombol Tutup (bawah panel) ───
    private void BuildBottomClose(Transform parent)
    {
        // Tombol besar agar mudah ditekan di VR
        GameObject closeBtn = CreatePanel(parent, "CloseBtn",
            new Vector2(400, 80),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            buttonSprite, closeBtnColor);
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

        Button closeBtnComp = closeBtn.AddComponent<Button>();
        Image closeBtnImg = closeBtn.GetComponent<Image>();
        ColorBlock ccb = closeBtnComp.colors;
        ccb.normalColor = closeBtnColor;
        ccb.highlightedColor = closeBtnColor * 1.15f;
        ccb.pressedColor = closeBtnColor * 0.85f;
        closeBtnComp.colors = ccb;
        closeBtnComp.targetGraphic = closeBtnImg;
        closeBtnComp.onClick.AddListener(() => ClosePoster());

        Text closeBtnLabel = CreateText(closeBtn.transform, "CloseBtnLabel",
            "\u2715  Tutup", 28, TextAnchor.MiddleCenter, Color.white);
        closeBtnLabel.fontStyle = FontStyle.Bold;
        StretchRect(closeBtnLabel.rectTransform, 10, 4, -10, -4);
    }

    // ═════════════════════════════════════════════
    //  HELPER METHODS
    // ═════════════════════════════════════════════

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 size, Vector2 anchorMin, Vector2 anchorMax,
        Sprite sprite, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        img.color = color;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return obj;
    }

    private Text CreateText(Transform parent, string name, string content,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private void StretchRect(RectTransform rect,
        float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    // ═════════════════════════════════════════════
    //  TRIGGER DETECTION (collider-based)
    // ═════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VRWalkController>() != null)
        {
            isPlayerNearby = true;
            if (!isPosterOpen && hintPanel != null) hintPanel.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<VRWalkController>() != null)
        {
            isPlayerNearby = false;
            if (hintPanel != null) hintPanel.SetActive(false);
            if (isPosterOpen) ClosePoster();
        }
    }
}