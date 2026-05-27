using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script Gaze Dialog
/// - Pasang script ini pada Object (misal: Dokter, Pasien, atau Item) yang sudah memiliki Collider.
/// - Isi field "Dialog Panel" dengan GameObject UI yang ingin dimunculkan.
/// - Saat player menatap (mengarahkan kursor/tengah layar) ke object ini, panel akan muncul otomatis.
/// - Saat player memalingkan pandangan, panel akan menghilang otomatis.
/// </summary>
public class GazeDialog : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Drag GameObject panel dialog/teks yang ingin ditampilkan")]
    [SerializeField] private GameObject dialogPanel;

    [Header("Interaction Settings")]
    [Tooltip("Jarak maksimal player bisa melihat dialog ini")]
    [SerializeField] private float viewDistance = 5.0f;
    [Tooltip("Waktu transisi fade in/out")]
    [SerializeField] private float fadeDuration = 0.2f;

    private bool isGazing = false;
    private bool isPanelActive = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Validasi Collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("[GazeDialog] Object " + gameObject.name + " membutuhkan Collider agar bisa dideteksi kursor!");
        }

        // Setup Panel & CanvasGroup
        if (dialogPanel != null)
        {
            canvasGroup = dialogPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialogPanel.AddComponent<CanvasGroup>();
            }

            // Sembunyikan panel di awal
            canvasGroup.alpha = 0f;
            dialogPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[GazeDialog] Dialog Panel belum di-assign pada " + gameObject.name);
        }
    }

    void Update()
    {
        if (mainCamera == null || dialogPanel == null) return;

        // Cek Gaze (Raycast dari tengah layar/kamera)
        bool currentlyGazing = CheckGaze();

        // Jika status gaze berubah
        if (currentlyGazing && !isGazing)
        {
            isGazing = true;
            ShowDialog();
        }
        else if (!currentlyGazing && isGazing)
        {
            isGazing = false;
            HideDialog();
        }
    }

    /// <summary>
    /// Melakukan Raycast dari tengah kamera untuk mengecek apakah melihat object ini
    /// </summary>
    private bool CheckGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // Tembakkan raycast sejauh viewDistance
        if (Physics.Raycast(ray, out hit, viewDistance))
        {
            // Jika yang terkena raycast adalah object ini atau anaknya
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    private void ShowDialog()
    {
        if (isPanelActive) return;
        isPanelActive = true;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        dialogPanel.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeCanvas(1f)); // Fade In
    }

    private void HideDialog()
    {
        if (!isPanelActive) return;
        isPanelActive = false;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCanvas(0f, () => {
            dialogPanel.SetActive(false);
        })); // Fade Out
    }

    /// <summary>
    /// Coroutine untuk efek transisi Fade In / Fade Out yang mulus
    /// </summary>
    private IEnumerator FadeCanvas(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
