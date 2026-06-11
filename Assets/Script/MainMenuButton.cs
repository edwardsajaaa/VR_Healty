using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Script all-in-one untuk button di Main Menu.
/// Gabungan: SwitchScene + ExitGame + Gaze Hover Effect + Tap Interaction.
///
/// === CARA SETUP ===
/// 1. Pasang script ini pada setiap button (Play, About, Quit)
/// 2. Pilih "Button Action" di Inspector:
///    - LoadScene  → isi "Scene Name" dengan nama scene tujuan
///    - QuitGame   → akan menutup aplikasi
/// 3. Pastikan button punya Collider (Box Collider) agar terdeteksi gaze
/// 4. Pastikan scene tujuan sudah ada di Build Settings
///
/// Interaksi:
/// - Player arahkan cursor ke button → hover effect (scale + warna berubah)
/// - Tap layar → trigger aksi button
/// - Opsional: gaze timer (otomatis trigger setelah menatap sekian detik)
/// </summary>
public class MainMenuButton : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════
    //  PENGATURAN AKSI
    // ═══════════════════════════════════════════════════════════════════

    public enum ButtonAction
    {
        LoadScene,
        QuitGame
    }

    [Header("Aksi Button")]
    [Tooltip("Pilih aksi yang dilakukan saat button ditekan")]
    public ButtonAction buttonAction = ButtonAction.LoadScene;

    [Tooltip("Nama scene tujuan (hanya untuk LoadScene, harus ada di Build Settings)")]
    public string sceneName;

    // ═══════════════════════════════════════════════════════════════════
    //  PENGATURAN HOVER / VISUAL
    // ═══════════════════════════════════════════════════════════════════

    [Header("Visual Hover")]
    [Tooltip("Warna button saat normal")]
    public Color normalColor = Color.white;

    [Tooltip("Warna button saat di-gaze / hover")]
    public Color hoverColor = new Color(0.75f, 0.92f, 1f);

    [Tooltip("Warna button saat diklik / ditekan")]
    public Color pressedColor = new Color(0.5f, 0.82f, 1f);

    [Tooltip("Skala button saat di-hover (1 = normal, 1.08 = sedikit membesar)")]
    [Range(1f, 1.3f)]
    public float hoverScale = 1.08f;

    [Tooltip("Durasi animasi transisi hover (detik)")]
    public float animDuration = 0.15f;

    [Header("Pengaturan Deteksi (Gaze)")]
    [Tooltip("Jarak maksimal button bisa dideteksi oleh pointer/pandangan")]
    public float maxGazeDistance = 100f;

    // ═══════════════════════════════════════════════════════════════════
    //  GAZE TIMER (OPSIONAL)
    // ═══════════════════════════════════════════════════════════════════

    [Header("Gaze Timer (Opsional)")]
    [Tooltip("Jika true, button aktif otomatis setelah player menatap selama gazeTime detik")]
    public bool useGazeTimer = false;

    [Range(0.5f, 5f)]
    public float gazeTime = 2f;

    [Tooltip("Fill image untuk progress lingkaran (opsional)")]
    public Image gazeProgressImage;

    // ═══════════════════════════════════════════════════════════════════
    //  STATE INTERNAL
    // ═══════════════════════════════════════════════════════════════════

    private bool isGazing = false;
    private float gazeTimer = 0f;
    private bool hasTriggeredThisGaze = false;

    private Image buttonImage;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private Coroutine colorCoroutine;
    private Camera mainCamera;

    // ═══════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;
        mainCamera = Camera.main;

        // Auto-tambah Box Collider jika belum ada (untuk raycast gaze)
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
                col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
        }

        // Inisialisasi warna
        if (buttonImage != null)
            buttonImage.color = normalColor;

        // Sembunyikan progress
        if (gazeProgressImage != null)
            gazeProgressImage.fillAmount = 0f;
    }

    void Update()
    {
        if (mainCamera == null) { mainCamera = Camera.main; return; }

        bool currentlyGazing = CheckGaze();

        // Masuk hover
        if (currentlyGazing && !isGazing)
        {
            isGazing = true;
            hasTriggeredThisGaze = false;
            gazeTimer = 0f;
            OnGazeEnter();
        }
        // Keluar hover
        else if (!currentlyGazing && isGazing)
        {
            isGazing = false;
            gazeTimer = 0f;
            OnGazeExit();
        }

        // Saat sedang di-gaze
        if (isGazing && !hasTriggeredThisGaze)
        {
            // Gaze timer (opsional)
            if (useGazeTimer)
            {
                gazeTimer += Time.deltaTime;
                if (gazeProgressImage != null)
                    gazeProgressImage.fillAmount = Mathf.Clamp01(gazeTimer / gazeTime);

                if (gazeTimer >= gazeTime)
                {
                    ExecuteAction();
                    return;
                }
            }

            // Tap layar
            if (DetectTap())
                ExecuteAction();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GAZE DETECTION
    // ═══════════════════════════════════════════════════════════════════

    private bool CheckGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxGazeDistance))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TAP DETECTION (ANDROID TOUCH + EDITOR MOUSE)
    // ═══════════════════════════════════════════════════════════════════

    private bool DetectTap()
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

        // Android touch (menggunakan New Input System)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            int fingerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId))
                return false;
            return true;
        }

        // Editor fallback: klik kiri mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HOVER EFFECTS
    // ═══════════════════════════════════════════════════════════════════

    private void OnGazeEnter()
    {
        AnimateScale(originalScale * hoverScale, animDuration);
        AnimateColor(hoverColor, animDuration);
    }

    private void OnGazeExit()
    {
        AnimateScale(originalScale, animDuration);
        AnimateColor(normalColor, animDuration);

        if (gazeProgressImage != null)
            gazeProgressImage.fillAmount = 0f;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AKSI BUTTON
    // ═══════════════════════════════════════════════════════════════════

    private void ExecuteAction()
    {
        hasTriggeredThisGaze = true;
        StartCoroutine(PressAndExecute());
    }

    private IEnumerator PressAndExecute()
    {
        // Animasi tekan
        AnimateScale(originalScale * 0.94f, 0.07f);
        AnimateColor(pressedColor, 0.07f);
        yield return new WaitForSeconds(0.12f);

        // Kembali ke hover
        AnimateScale(originalScale * hoverScale, 0.07f);
        AnimateColor(hoverColor, 0.07f);
        yield return new WaitForSeconds(0.1f);

        // Jalankan aksi
        switch (buttonAction)
        {
            case ButtonAction.LoadScene:
                LoadScene();
                break;

            case ButtonAction.QuitGame:
                QuitGame();
                break;
        }
    }

    // ─── Load Scene ───────────────────────────────────────────────────

    /// <summary>
    /// Berpindah scene berdasarkan nama yang diisi di Inspector.
    /// Bisa dipanggil dari onClick Button atau otomatis dari gaze/tap.
    /// </summary>
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log("Memuat scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Nama scene tidak diisi pada " + gameObject.name + "!");
        }
    }

    /// <summary>
    /// Berpindah scene dengan parameter nama (untuk pemanggilan dari script lain)
    /// </summary>
    public void LoadSceneByName(string targetSceneName)
    {
        if (!string.IsNullOrEmpty(targetSceneName))
            SceneManager.LoadScene(targetSceneName);
        else
            Debug.LogWarning("Nama scene tidak valid!");
    }

    /// <summary>
    /// Berpindah scene berdasarkan index di Build Settings
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Reload scene yang sedang aktif
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // ─── Quit Game ────────────────────────────────────────────────────

    /// <summary>
    /// Keluar dari aplikasi
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Keluar dari game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ANIMATION HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private void AnimateScale(Vector3 targetScale, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale, duration));
    }

    private void AnimateColor(Color targetColor, float duration)
    {
        if (buttonImage == null) return;
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(ColorTo(targetColor, duration));
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator ColorTo(Color target, float duration)
    {
        Color start = buttonImage.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            buttonImage.color = Color.Lerp(start, target, t);
            yield return null;
        }
        buttonImage.color = target;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═══════════════════════════════════════════════════════════════════

    void OnDisable()
    {
        transform.localScale = originalScale;
        if (buttonImage != null) buttonImage.color = normalColor;
        if (gazeProgressImage != null) gazeProgressImage.fillAmount = 0f;
        isGazing = false;
        gazeTimer = 0f;
    }
}
