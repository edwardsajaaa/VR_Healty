using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GazeDialog : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "Ners";

    [Header("Interaction")]
    [SerializeField] private float viewDistance = 5.0f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float canvasDistance = 0.8f;
    [SerializeField] private float canvasScale = 0.001f;
    [Tooltip("Kecepatan efek mengetik teks (detik/karakter).")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Voice Over (Opsional)")]
    [Tooltip("AudioSource untuk memutar suara ners. Jika kosong, akan dibuat otomatis.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Masukkan rekaman suara jawaban ners secara berurutan sesuai pertanyaan.")]
    [SerializeField] private AudioClip[] answerAudios;

    [Header("BGM Ducking (Opsional)")]
    [Tooltip("Masukkan AudioSource musik latar/BGM jika ingin suaranya mengecil saat ners bicara.")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("Pengali volume BGM saat ners bicara (misal 0.2 untuk 20% dari volume asli).")]
    [Range(0f, 1f)]
    [SerializeField] private float duckedVolume = 0.2f;

    private float originalBgmVolume = 1f;
    private bool isDucking = false;

    [Header("Custom UI Sprites")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;

    [Header("Warna UI")]
    [SerializeField] private Color panelColor = Color.white;
    [SerializeField] private Color buttonColor = Color.white;
    [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.92f, 1f);
    [SerializeField] private Color textColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color headerColor = new Color(0.08f, 0.35f, 0.55f);
    [SerializeField] private Color headerTextColor = Color.white;

    private bool isGazingAtNPC = false;
    private bool isDialogOpen = false;
    private Camera mainCamera;
    private VRWalkController playerController;

    private Canvas dialogCanvas;
    private CanvasGroup canvasGroup;
    private GameObject hintPanel;
    private GameObject mainDialogPanel;
    private GameObject questionListPanel;
    private GameObject answerPanel;
    private Text answerText;
    private Coroutine fadeCoroutine;
    private Coroutine typingCoroutine;

    [Header("Input System")]
    public InputAction interactAction = new InputAction("Interact", InputActionType.Button);
    public InputAction cancelAction = new InputAction("Cancel", InputActionType.Button);

    private List<QAData> qaList = new List<QAData>();

    [System.Serializable]
    private class QAData
    {
        public string question;
        public string answer;
        public QAData(string q, string a) { question = q; answer = a; }
    }

    void Awake()
    {
        if (cancelAction.bindings.Count == 0)
        {
            cancelAction.AddBinding("<Gamepad>/buttonEast"); // Tombol B
            cancelAction.AddBinding("<XRController>/secondaryButton");
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Keyboard>/b");
        }
        if (interactAction.bindings.Count == 0)
        {
            interactAction.AddBinding("<XRController>/triggerPressed");
            interactAction.AddBinding("<XRController>/primaryButton"); // Tombol A
            interactAction.AddBinding("<Gamepad>/buttonSouth"); // Tombol A
            interactAction.AddBinding("<Keyboard>/c");
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

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (bgmSource != null)
        {
            originalBgmVolume = bgmSource.volume;
        }

        InitializeQAData();
        BuildUI();
    }

    void Update()
    {
        if (mainCamera == null) return;

        bool gazing = CheckGaze();

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

        // Buka dialog dengan tombol controller
        if (isGazingAtNPC && !isDialogOpen && DetectControllerInteract())
            OpenDialog();

        // Tutup dialog dengan tombol B / Cancel
        if (isDialogOpen && cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            CloseDialog();
        }

        // Kembalikan volume BGM jika suara ners sudah selesai bicara
        if (isDucking && audioSource != null && !audioSource.isPlaying)
        {
            RestoreBGM();
        }
    }

    private void RestoreBGM()
    {
        if (isDucking && bgmSource != null)
        {
            bgmSource.volume = originalBgmVolume;
            isDucking = false;
        }
    }

    private bool DetectControllerInteract()
    {
        // Universal fallback: tekan tombol apa saja di gamepad/joystick
        if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            return true;

        if (interactAction != null && interactAction.WasPressedThisFrame())
            return true;

        // Editor / PC fallback: klik kiri mouse
        if (Input.GetMouseButtonDown(0))
            return true;

        // Android Touch fallback
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
            return true;

        return false;
    }

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

    private bool CheckGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, viewDistance))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    private void OpenDialog()
    {
        isDialogOpen = true;
        if (hintPanel != null) hintPanel.SetActive(false);

        ShowQuestionList();
        mainDialogPanel.SetActive(true);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f));

        if (playerController != null) playerController.LockMovement();

        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = true;
        }
    }

    private void CloseDialog()
    {
        isDialogOpen = false;
        isGazingAtNPC = false;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        RestoreBGM();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, () => {
            mainDialogPanel.SetActive(false);
        }));

        if (playerController != null) playerController.UnlockMovement();
    }

    private void ShowQuestionList()
    {
        questionListPanel.SetActive(true);
        answerPanel.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        RestoreBGM();

        // Auto-select pertanyaan pertama agar bisa dikendalikan analog
        StartCoroutine(SelectFirstQuestionDelay());
    }

    private IEnumerator SelectFirstQuestionDelay()
    {
        yield return null;
        if (questionListPanel != null && questionListPanel.activeInHierarchy)
        {
            Button[] btns = questionListPanel.GetComponentsInChildren<Button>();
            if (btns.Length > 0 && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(btns[0].gameObject);
            }
        }
    }

    private void ShowAnswer(int index)
    {
        questionListPanel.SetActive(false);
        answerPanel.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (typingSpeed > 0f)
            typingCoroutine = StartCoroutine(TypeAnswer(qaList[index].answer));
        else
            answerText.text = qaList[index].answer;

        if (audioSource != null && answerAudios != null && index < answerAudios.Length && answerAudios[index] != null)
        {
            audioSource.clip = answerAudios[index];
            audioSource.Play();

            if (bgmSource != null && !isDucking)
            {
                originalBgmVolume = bgmSource.volume;
                bgmSource.volume = originalBgmVolume * duckedVolume;
                isDucking = true;
            }
        }

        // Auto-select tombol "Kembali" agar bisa dinavigasi pakai analog
        StartCoroutine(SelectBackButtonDelay());
    }

    private IEnumerator SelectBackButtonDelay()
    {
        yield return null;
        if (answerPanel != null && answerPanel.activeInHierarchy)
        {
            Button[] btns = answerPanel.GetComponentsInChildren<Button>();
            if (btns.Length > 0 && EventSystem.current != null)
            {
                // Pilih tombol pertama (Biasanya "Kembali ke Pertanyaan")
                EventSystem.current.SetSelectedGameObject(btns[0].gameObject);
            }
        }
    }

    private IEnumerator TypeAnswer(string fullText)
    {
        answerText.text = "";
        for (int i = 0; i <= fullText.Length; i++)
        {
            answerText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
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

    private void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("DialogCanvas_" + npcName);
        dialogCanvas = canvasObj.AddComponent<Canvas>();
        
        // WorldSpace agar berfungsi di VR Cardboard
        dialogCanvas.renderMode = RenderMode.WorldSpace;
        canvasObj.transform.SetParent(mainCamera.transform, false);

        // Posisikan canvas lebih dekat ke kamera
        canvasObj.transform.localPosition = new Vector3(0, 0, canvasDistance);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);

        dialogCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        hintPanel = CreateSpritePanel(canvasObj.transform, "HintPanel",
            new Vector2(600, 70), new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f),
            buttonSprite, panelColor);

        Text hintText = CreateText(hintPanel.transform, "HintText",
            "Tekan 'C' untuk berbicara dengan " + npcName,
            28, TextAnchor.MiddleCenter, textColor);
        StretchRect(hintText.rectTransform, 15, 5, -15, -5);
        hintPanel.SetActive(false);

        mainDialogPanel = CreateSpritePanel(canvasObj.transform, "MainDialogPanel",
            new Vector2(1050, 850), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            panelSprite, panelColor);

        canvasGroup = mainDialogPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        mainDialogPanel.SetActive(false);

        GameObject headerBar = CreateSpritePanel(mainDialogPanel.transform, "HeaderBar",
            Vector2.zero, Vector2.zero, Vector2.zero, buttonSprite, headerColor);
        RectTransform hbRect = headerBar.GetComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0, 1);
        hbRect.anchorMax = new Vector2(1, 1);
        hbRect.pivot = new Vector2(0.5f, 1);
        hbRect.sizeDelta = new Vector2(-20, 75);
        hbRect.anchoredPosition = new Vector2(0, -10);

        Text nameText = CreateText(headerBar.transform, "NameText", npcName,
            40, TextAnchor.MiddleLeft, headerTextColor);
        nameText.fontStyle = FontStyle.Bold;
        RectTransform ntRect = nameText.rectTransform;
        ntRect.anchorMin = Vector2.zero;
        ntRect.anchorMax = new Vector2(0.8f, 1);
        ntRect.offsetMin = new Vector2(25, 0);
        ntRect.offsetMax = Vector2.zero;

        CreateSpriteButton(headerBar.transform, "CloseX", "✕",
            new Vector2(60, 50), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0),
            buttonSprite, new Color(0.9f, 0.25f, 0.25f), Color.white, 28, () => { CloseDialog(); });

        questionListPanel = new GameObject("QuestionListPanel");
        questionListPanel.transform.SetParent(mainDialogPanel.transform, false);
        RectTransform qlRect = questionListPanel.AddComponent<RectTransform>();
        qlRect.anchorMin = Vector2.zero;
        qlRect.anchorMax = Vector2.one;
        qlRect.offsetMin = new Vector2(25, 25);
        qlRect.offsetMax = new Vector2(-25, -95);

        Text qlLabel = CreateText(questionListPanel.transform, "Label",
            "Pilih pertanyaan:", 30, TextAnchor.MiddleLeft, new Color(0.3f, 0.3f, 0.3f));
        qlLabel.fontStyle = FontStyle.Bold;
        RectTransform lblRect = qlLabel.rectTransform;
        lblRect.anchorMin = new Vector2(0, 1);
        lblRect.anchorMax = new Vector2(1, 1);
        lblRect.pivot = new Vector2(0.5f, 1);
        lblRect.sizeDelta = new Vector2(0, 38);
        lblRect.anchoredPosition = Vector2.zero;

        float btnY = -45f;
        float btnH = 95f;
        float btnGap = 12f;

        for (int i = 0; i < qaList.Count; i++)
        {
            int idx = i;
            float yPos = btnY - (i * (btnH + btnGap));

            GameObject btnObj = CreateSpritePanel(questionListPanel.transform, "QBtn_" + i,
                new Vector2(0, btnH), Vector2.zero, Vector2.zero, buttonSprite, buttonColor);
            RectTransform bRect = btnObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.sizeDelta = new Vector2(0, btnH);
            bRect.anchoredPosition = new Vector2(0, yPos);

            Text numText = CreateText(btnObj.transform, "Num", (i + 1) + ".",
                28, TextAnchor.UpperLeft, headerColor);
            numText.fontStyle = FontStyle.Bold;
            RectTransform nRect = numText.rectTransform;
            nRect.anchorMin = Vector2.zero;
            nRect.anchorMax = new Vector2(0, 1);
            nRect.pivot = new Vector2(0, 0.5f);
            nRect.offsetMin = new Vector2(15, 10);
            nRect.offsetMax = new Vector2(45, -10);

            Text qText = CreateText(btnObj.transform, "QText", qaList[i].question,
                26, TextAnchor.UpperLeft, textColor);
            RectTransform qRect = qText.rectTransform;
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.offsetMin = new Vector2(50, 8);
            qRect.offsetMax = new Vector2(-15, -8);

            Button btn = btnObj.AddComponent<Button>();
            Image btnImg = btnObj.GetComponent<Image>();
            ColorBlock cb = btn.colors;
            cb.normalColor = buttonColor;
            cb.highlightedColor = buttonHoverColor;
            cb.pressedColor = new Color(0.75f, 0.88f, 1f);
            cb.selectedColor = buttonHoverColor;
            btn.colors = cb;
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => { ShowAnswer(idx); });
        }

        answerPanel = new GameObject("AnswerPanel");
        answerPanel.transform.SetParent(mainDialogPanel.transform, false);
        RectTransform apRect = answerPanel.AddComponent<RectTransform>();
        apRect.anchorMin = Vector2.zero;
        apRect.anchorMax = Vector2.one;
        apRect.offsetMin = new Vector2(25, 25);
        apRect.offsetMax = new Vector2(-25, -95);

        Text ansLabel = CreateText(answerPanel.transform, "AnsLabel",
            npcName + " menjawab:", 30, TextAnchor.MiddleLeft, headerColor);
        ansLabel.fontStyle = FontStyle.Bold;
        RectTransform alRect = ansLabel.rectTransform;
        alRect.anchorMin = new Vector2(0, 1);
        alRect.anchorMax = new Vector2(1, 1);
        alRect.pivot = new Vector2(0.5f, 1);
        alRect.sizeDelta = new Vector2(0, 38);
        alRect.anchoredPosition = Vector2.zero;

        GameObject ansBox = CreateSpritePanel(answerPanel.transform, "AnsBox",
            Vector2.zero, Vector2.zero, Vector2.zero, panelSprite, new Color(0.96f, 0.96f, 0.96f));
        RectTransform abRect = ansBox.GetComponent<RectTransform>();
        abRect.anchorMin = new Vector2(0, 0.18f);
        abRect.anchorMax = Vector2.one;
        abRect.offsetMin = new Vector2(0, 0);
        abRect.offsetMax = new Vector2(0, -40);

        answerText = CreateText(ansBox.transform, "AnsText", "",
            28, TextAnchor.UpperLeft, textColor);
        answerText.lineSpacing = 1.4f;
        RectTransform atRect = answerText.rectTransform;
        atRect.anchorMin = Vector2.zero;
        atRect.anchorMax = Vector2.one;
        atRect.offsetMin = new Vector2(25, 20);
        atRect.offsetMax = new Vector2(-25, -20);

        CreateSpriteButton(answerPanel.transform, "BackBtn", "← Kembali ke Pertanyaan",
            new Vector2(360, 55), new Vector2(0, 0), new Vector2(0, 0), new Vector2(180, 28),
            buttonSprite, headerColor, headerTextColor, 24, () => { ShowQuestionList(); });

        CreateSpriteButton(answerPanel.transform, "CloseBtn2", "✕ Tutup",
            new Vector2(200, 55), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-100, 28),
            buttonSprite, new Color(0.7f, 0.2f, 0.2f), Color.white, 24, () => { CloseDialog(); });

        answerPanel.SetActive(false);
    }

    private GameObject CreateSpritePanel(Transform parent, string name,
        Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, Color color)
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

    private void CreateSpriteButton(Transform parent, string name, string label,
        Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
        Sprite sprite, Color bgColor, Color txtColor, int fontSize,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateSpritePanel(parent, name, size, anchorMin, anchorMax, sprite, bgColor);
        btnObj.GetComponent<RectTransform>().anchoredPosition = position;

        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.GetComponent<Image>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.15f;
        cb.pressedColor = bgColor * 0.85f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        Text text = CreateText(btnObj.transform, "Label", label, fontSize,
            TextAnchor.MiddleCenter, txtColor);
        StretchRect(text.rectTransform, 8, 4, -8, -4);
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

    private void StretchRect(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
