using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script Dialog Q&A dengan NPC
/// - Pasang pada GameObject NPC (Dokter, dll) yang memiliki Collider
/// - Arahkan kursor ke NPC → muncul teks "Tekan E untuk berbicara"
/// - Tekan E → Panel dialog Q&A muncul dengan daftar pertanyaan
/// - Klik pertanyaan → NPC menjawab
/// - Tombol "Kembali" → kembali ke daftar pertanyaan
/// - Tekan Q atau klik "Tutup" → keluar dari dialog
/// - Semua UI dibuat otomatis melalui kode, tidak perlu setup manual
/// </summary>
public class GazeDialog : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "Dokter";

    [Header("Interaction")]
    [SerializeField] private float viewDistance = 5.0f;
    [SerializeField] private float fadeDuration = 0.25f;

    // ─── State ───
    private bool isGazingAtNPC = false;
    private bool isDialogOpen = false;
    private Camera mainCamera;
    private VRWalkController playerController;

    // ─── UI References (dibuat otomatis) ───
    private Canvas dialogCanvas;
    private CanvasGroup canvasGroup;
    private GameObject hintPanel;          // "Tekan E untuk berbicara"
    private GameObject mainDialogPanel;    // Panel utama dialog
    private GameObject questionListPanel;  // Daftar pertanyaan
    private GameObject answerPanel;        // Panel jawaban NPC
    private Text answerText;
    private Text npcNameText;
    private Coroutine fadeCoroutine;

    // ─── Data Q&A ───
    private List<QAData> qaList = new List<QAData>();

    [System.Serializable]
    private class QAData
    {
        public string question;
        public string answer;

        public QAData(string q, string a)
        {
            question = q;
            answer = a;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        playerController = FindObjectOfType<VRWalkController>();

        // Validasi Collider
        if (GetComponent<Collider>() == null)
            Debug.LogError("[GazeDialog] " + gameObject.name + " membutuhkan Collider!");

        // Isi data Q&A
        InitializeQAData();

        // Buat semua UI
        BuildUI();
    }

    void Update()
    {
        if (mainCamera == null) return;

        bool gazing = CheckGaze();

        // ─── Hint muncul/hilang saat menatap NPC ───
        if (gazing && !isGazingAtNPC && !isDialogOpen)
        {
            isGazingAtNPC = true;
            if (hintPanel != null) hintPanel.SetActive(true);
        }
        else if (!gazing && isGazingAtNPC && !isDialogOpen)
        {
            isGazingAtNPC = false;
            if (hintPanel != null) hintPanel.SetActive(false);
        }

        // ─── Tekan E → Buka Dialog ───
        if (isGazingAtNPC && Input.GetKeyDown(KeyCode.E) && !isDialogOpen)
        {
            OpenDialog();
        }

        // ─── Tekan Q → Tutup Dialog ───
        if (isDialogOpen && Input.GetKeyDown(KeyCode.Q))
        {
            CloseDialog();
        }
    }

    // ════════════════════════════════════════════
    //  INISIALISASI DATA Q&A
    // ════════════════════════════════════════════

    private void InitializeQAData()
    {
        qaList.Add(new QAData(
            "Berapa usia ideal menikah?",
            "Nikah ideal adalah Perempuan 21 tahun dan laki-laki 25 tahun, karena pada usia ini sistem reproduksi seseorang sudah siap untuk melaksanakan tugasnya."
        ));

        qaList.Add(new QAData(
            "Apakah hubungan seks dengan kondom bisa mencegah penyakit menular seksual?",
            "Kondom tidak bisa melindungi 100% dari penyakit infeksi kelamin, namun bila digunakan dengan benar dan konsisten, alat kontrasepsi tersebut mampu mencegah penyakit menular seksual tersebut secara efektif."
        ));

        qaList.Add(new QAData(
            "Apakah senggama terputus (keluar diluar) tidak menyebabkan kehamilan?",
            "Membuang sperma diluar pada dasarnya tidak dapat menjamin pasti tidak terjadi kehamilan. Selain itu sebelum terjadinya ejakulasi seorang pria dapat mengeluarkan cairan pre-cum yang dapat mengandung sedikit sperma."
        ));

        qaList.Add(new QAData(
            "Apakah masturbasi/onani berbahaya?",
            "Masturbasi memang tidak menyebabkan infeksi menular seksual, tetapi kamu bisa mengalami iritasi alat kelamin jika melakukan secara berlebihan. Iritasi pada alat kelamin ini akan memunculkan rasa gatal, kulit tampak bersisik, kemerahan, serta rasa perih atau nyeri."
        ));

        qaList.Add(new QAData(
            "Apakah berciuman atau memberikan rangsangan dengan mulut pada pasangan berisiko menularkan HIV-AIDS?",
            "Berciuman dengan pengidap HIV tidak meningkatkan risiko tertular HIV, tetapi jika kamu berciuman dengan pengidap yang memiliki luka dalam mulutnya virus akan masuk ke aliran darah dan menginfeksi tubuh. Dengan demikian kamu akan bisa langsung tertular."
        ));

        qaList.Add(new QAData(
            "Apakah berhubungan badan sekali tidak menyebabkan hamil?",
            "Meski hanya sekali melakukan seks dengan pasangan dapat menyebabkan kehamilan selama melakukannya di saat yang tepat, seorang wanita bisa hamil ketika baru sekali berhubungan intim dengan pasangannya."
        ));
    }

    // ════════════════════════════════════════════
    //  RAYCAST GAZE
    // ════════════════════════════════════════════

    private bool CheckGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, viewDistance))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    // ════════════════════════════════════════════
    //  BUKA / TUTUP DIALOG
    // ════════════════════════════════════════════

    private void OpenDialog()
    {
        isDialogOpen = true;
        if (hintPanel != null) hintPanel.SetActive(false);

        // Tampilkan panel pertanyaan
        ShowQuestionList();

        // Aktifkan canvas & fade in
        mainDialogPanel.SetActive(true);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f));

        // Kunci gerakan player
        if (playerController != null)
            playerController.LockMovement();

        Debug.Log("[GazeDialog] Dialog dibuka dengan " + npcName);
    }

    private void CloseDialog()
    {
        isDialogOpen = false;
        isGazingAtNPC = false;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, () =>
        {
            mainDialogPanel.SetActive(false);
        }));

        // Buka kembali gerakan player
        if (playerController != null)
            playerController.UnlockMovement();

        Debug.Log("[GazeDialog] Dialog ditutup");
    }

    private void ShowQuestionList()
    {
        questionListPanel.SetActive(true);
        answerPanel.SetActive(false);
    }

    private void ShowAnswer(int index)
    {
        questionListPanel.SetActive(false);
        answerPanel.SetActive(true);
        answerText.text = qaList[index].answer;
    }

    // ════════════════════════════════════════════
    //  FADE
    // ════════════════════════════════════════════

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

    // ════════════════════════════════════════════
    //  BUILD UI (SEMUA MELALUI KODE)
    // ════════════════════════════════════════════

    private void BuildUI()
    {
        // ─── Pastikan EventSystem ada ───
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // ─── Canvas ───
        GameObject canvasObj = new GameObject("DialogCanvas_" + npcName);
        dialogCanvas = canvasObj.AddComponent<Canvas>();
        dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ─── Hint Panel ("Tekan E untuk berbicara") ───
        hintPanel = CreatePanel(canvasObj.transform, "HintPanel", new Vector2(400, 50),
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Color(0, 0, 0, 0.7f));
        Text hintText = CreateText(hintPanel.transform, "HintText", "Tekan E untuk berbicara dengan " + npcName,
            16, TextAnchor.MiddleCenter, Color.white);
        SetAnchorsStretch(hintText.rectTransform, 10);
        hintPanel.SetActive(false);

        // ─── Main Dialog Panel (berisi semua) ───
        mainDialogPanel = CreatePanel(canvasObj.transform, "MainDialogPanel", new Vector2(900, 600),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.12f, 0.12f, 0.18f, 0.95f));

        canvasGroup = mainDialogPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        mainDialogPanel.SetActive(false);

        // ─── Header (nama NPC + tombol tutup) ───
        GameObject headerPanel = CreatePanel(mainDialogPanel.transform, "HeaderPanel", Vector2.zero,
            Vector2.zero, Vector2.zero, new Color(0.08f, 0.35f, 0.55f, 1f));
        RectTransform headerRect = headerPanel.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 60);
        headerRect.anchoredPosition = Vector2.zero;

        // Nama NPC
        npcNameText = CreateText(headerPanel.transform, "NPCNameText", npcName,
            24, TextAnchor.MiddleLeft, Color.white);
        RectTransform nameRect = npcNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(0.8f, 1);
        nameRect.offsetMin = new Vector2(20, 0);
        nameRect.offsetMax = new Vector2(0, 0);

        // Tombol Tutup (X)
        CreateButton(headerPanel.transform, "CloseBtn", "✕", new Vector2(50, 40),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-35, 0),
            new Color(0.8f, 0.2f, 0.2f, 1f), Color.white, 22, () => { CloseDialog(); });

        // ─── Question List Panel ───
        questionListPanel = CreatePanel(mainDialogPanel.transform, "QuestionListPanel", Vector2.zero,
            Vector2.zero, Vector2.zero, Color.clear);
        RectTransform qlRect = questionListPanel.GetComponent<RectTransform>();
        qlRect.anchorMin = new Vector2(0, 0);
        qlRect.anchorMax = new Vector2(1, 1);
        qlRect.offsetMin = new Vector2(20, 20);
        qlRect.offsetMax = new Vector2(-20, -70);

        // Label "Pilih Pertanyaan"
        Text labelText = CreateText(questionListPanel.transform, "LabelText", "Pilih pertanyaan:",
            18, TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.sizeDelta = new Vector2(0, 35);
        labelRect.anchoredPosition = Vector2.zero;

        // Tombol pertanyaan
        float btnStartY = -40f;
        float btnHeight = 65f;
        float btnSpacing = 8f;

        for (int i = 0; i < qaList.Count; i++)
        {
            int idx = i; // Capture index untuk closure
            float yPos = btnStartY - (i * (btnHeight + btnSpacing));

            GameObject btnObj = CreatePanel(questionListPanel.transform, "QBtn_" + i, new Vector2(0, btnHeight),
                Vector2.zero, Vector2.zero, new Color(0.18f, 0.22f, 0.32f, 1f));
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.sizeDelta = new Vector2(0, btnHeight);
            btnRect.anchoredPosition = new Vector2(0, yPos);

            // Nomor pertanyaan
            Text numText = CreateText(btnObj.transform, "NumText", (i + 1).ToString() + ".",
                16, TextAnchor.UpperLeft, new Color(0.4f, 0.75f, 1f));
            RectTransform numRect = numText.rectTransform;
            numRect.anchorMin = new Vector2(0, 0);
            numRect.anchorMax = new Vector2(0, 1);
            numRect.pivot = new Vector2(0, 0.5f);
            numRect.sizeDelta = new Vector2(30, 0);
            numRect.anchoredPosition = new Vector2(10, 0);
            numRect.offsetMin = new Vector2(10, 8);
            numRect.offsetMax = new Vector2(40, -8);

            // Teks pertanyaan
            Text qText = CreateText(btnObj.transform, "QText", qaList[i].question,
                15, TextAnchor.UpperLeft, Color.white);
            RectTransform qRect = qText.rectTransform;
            qRect.anchorMin = new Vector2(0, 0);
            qRect.anchorMax = new Vector2(1, 1);
            qRect.offsetMin = new Vector2(45, 8);
            qRect.offsetMax = new Vector2(-10, -8);

            // Button component
            Button btn = btnObj.AddComponent<Button>();
            Image btnImg = btnObj.GetComponent<Image>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.22f, 0.32f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.35f, 0.55f, 1f);
            colors.pressedColor = new Color(0.1f, 0.45f, 0.7f, 1f);
            colors.selectedColor = new Color(0.25f, 0.35f, 0.55f, 1f);
            btn.colors = colors;
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => { ShowAnswer(idx); });
        }

        // ─── Answer Panel ───
        answerPanel = CreatePanel(mainDialogPanel.transform, "AnswerPanel", Vector2.zero,
            Vector2.zero, Vector2.zero, Color.clear);
        RectTransform apRect = answerPanel.GetComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0, 0);
        apRect.anchorMax = new Vector2(1, 1);
        apRect.offsetMin = new Vector2(20, 20);
        apRect.offsetMax = new Vector2(-20, -70);

        // Label "Jawaban NPC"
        Text ansLabel = CreateText(answerPanel.transform, "AnswerLabel", npcName + " menjawab:",
            18, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 0.5f));
        RectTransform ansLabelRect = ansLabel.rectTransform;
        ansLabelRect.anchorMin = new Vector2(0, 1);
        ansLabelRect.anchorMax = new Vector2(1, 1);
        ansLabelRect.pivot = new Vector2(0.5f, 1);
        ansLabelRect.sizeDelta = new Vector2(0, 35);
        ansLabelRect.anchoredPosition = Vector2.zero;

        // Box jawaban
        GameObject answerBox = CreatePanel(answerPanel.transform, "AnswerBox", Vector2.zero,
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.18f, 0.25f, 1f));
        RectTransform abRect = answerBox.GetComponent<RectTransform>();
        abRect.anchorMin = new Vector2(0, 0.2f);
        abRect.anchorMax = new Vector2(1, 1);
        abRect.offsetMin = new Vector2(0, 0);
        abRect.offsetMax = new Vector2(0, -45);

        // Teks jawaban
        answerText = CreateText(answerBox.transform, "AnswerText", "",
            17, TextAnchor.UpperLeft, Color.white);
        RectTransform atRect = answerText.rectTransform;
        atRect.anchorMin = Vector2.zero;
        atRect.anchorMax = Vector2.one;
        atRect.offsetMin = new Vector2(20, 15);
        atRect.offsetMax = new Vector2(-20, -15);
        answerText.lineSpacing = 1.3f;

        // Tombol "Kembali ke Pertanyaan"
        CreateButton(answerPanel.transform, "BackBtn", "← Kembali ke Pertanyaan", new Vector2(280, 45),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(140, 22),
            new Color(0.08f, 0.35f, 0.55f, 1f), Color.white, 16, () => { ShowQuestionList(); });

        // Tombol "Tutup" di answer panel
        CreateButton(answerPanel.transform, "CloseBtn2", "Tutup (Q)", new Vector2(160, 45),
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 22),
            new Color(0.6f, 0.15f, 0.15f, 1f), Color.white, 16, () => { CloseDialog(); });

        answerPanel.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  UI HELPER METHODS
    // ════════════════════════════════════════════

    private GameObject CreatePanel(Transform parent, string name, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
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

    private void CreateButton(Transform parent, string name, string label, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
        Color bgColor, Color textColor, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        Text text = CreateText(btnObj.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, textColor);
        SetAnchorsStretch(text.rectTransform, 5);
    }

    private void SetAnchorsStretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}
