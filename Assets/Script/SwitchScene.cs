using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Script untuk berpindah scene melalui interaksi button.
/// Mendukung klik biasa (UI Button onClick) dan VR Gaze (Google Cardboard).
/// 
/// === CARA SETUP ===
/// 
/// 1. Pasang script ini pada GameObject button (misal: button "Play" di PanelMainMenu)
/// 2. Di Inspector, isi field "Scene Name" dengan nama scene tujuan (misal: "Gameplay")
/// 3. Pastikan scene tujuan sudah ditambahkan di Build Settings (File > Build Settings > Add Open Scenes)
/// 
/// -- Untuk UI Button biasa (onClick) --
/// 4a. Pada komponen Button, tambahkan onClick event
/// 5a. Drag GameObject yang memiliki script ini ke slot onClick
/// 6a. Pilih method: SwitchScene > LoadScene()
/// 
/// -- Untuk VR Gaze Interaction (Google Cardboard) --
/// 4b. Pastikan button memiliki komponen Collider (Box Collider) 
///     agar bisa di-raycast oleh CardboardReticlePointer
/// 5b. Centang "Use Gaze Timer" di Inspector
/// 6b. Atur "Gaze Time" (default 2 detik) — durasi player menatap sebelum scene berpindah
/// 7b. Script ini otomatis mendeteksi gaze dari kamera dan memicu perpindahan scene
/// </summary>
public class SwitchScene : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scene Settings")]
    [Tooltip("Nama scene yang ingin dituju (harus sudah ada di Build Settings)")]
    public string sceneName;

    [Header("VR Gaze Settings")]
    [Tooltip("Aktifkan timer gaze untuk VR — scene berpindah otomatis setelah player menatap selama beberapa detik")]
    public bool useGazeTimer = true;

    [Tooltip("Durasi (detik) player harus menatap button sebelum scene berpindah")]
    [Range(0.5f, 5f)]
    public float gazeTime = 2f;

    [Header("Visual Feedback (Opsional)")]
    [Tooltip("UI Image untuk loading indicator melingkar (opsional, bisa dikosongkan)")]
    public UnityEngine.UI.Image gazeLoadingImage;

    // State tracking
    private bool isGazing = false;
    private float gazeTimer = 0f;

    void Update()
    {
        if (!useGazeTimer) return;

        if (isGazing)
        {
            gazeTimer += Time.deltaTime;

            // Update loading indicator jika ada
            if (gazeLoadingImage != null)
            {
                gazeLoadingImage.fillAmount = gazeTimer / gazeTime;
            }

            // Jika sudah menatap cukup lama, pindah scene
            if (gazeTimer >= gazeTime)
            {
                LoadScene();
            }
        }
    }

    // === EVENT HANDLER UNTUK VR GAZE & POINTER ===

    /// <summary>
    /// Dipanggil saat pointer/gaze masuk ke area button
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isGazing = true;
        gazeTimer = 0f;

        if (gazeLoadingImage != null)
        {
            gazeLoadingImage.fillAmount = 0f;
            gazeLoadingImage.gameObject.SetActive(true);
        }

        Debug.Log("Gaze masuk ke button: " + gameObject.name);
    }

    /// <summary>
    /// Dipanggil saat pointer/gaze keluar dari area button
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isGazing = false;
        gazeTimer = 0f;

        if (gazeLoadingImage != null)
        {
            gazeLoadingImage.fillAmount = 0f;
            gazeLoadingImage.gameObject.SetActive(false);
        }

        Debug.Log("Gaze keluar dari button: " + gameObject.name);
    }

    /// <summary>
    /// Dipanggil saat pointer/gaze mengklik button (tap di layar / trigger Cardboard)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Button diklik: " + gameObject.name);
        LoadScene();
    }

    // === METHOD UNTUK BERPINDAH SCENE ===

    /// <summary>
    /// Berpindah scene berdasarkan nama yang sudah diisi di Inspector.
    /// Bisa dipanggil dari onClick Button atau otomatis dari gaze timer.
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
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("Nama scene tidak valid!");
        }
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

    /// <summary>
    /// Keluar dari aplikasi
    /// </summary>
    public void QuitApplication()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        Debug.Log("Aplikasi ditutup");
    }
}
