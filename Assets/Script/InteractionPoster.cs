using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script Interaksi Poster (dengan Efek Fade In/Out Premium)
/// - Pasang script ini pada GameObject Picture/Poster (yang sudah punya Box Collider)
/// - Isi field "Poster Panel" dengan GameObject Image/Panel UI yang ingin ditampilkan
/// - Tekan E saat dekat poster → panel aktif dengan transisi Fade In
/// - Tekan Q untuk menutup panel dengan transisi Fade Out
/// </summary>
public class InteractionPoster : MonoBehaviour
{
    [Header("Poster Settings")]
    [SerializeField] private string posterTitle = "Poster";

    [Header("Panel UI")]
    [Tooltip("Drag GameObject Image/Panel dari CanvasUI yang ingin muncul saat E ditekan")]
    [SerializeField] private GameObject posterPanel;

    [Header("Interaksi")]
    [SerializeField] private float interactionDistance = 3.5f;
    [Tooltip("Aktifkan: harus menatap poster dulu | Nonaktifkan: cukup dekat saja")]
    [SerializeField] private bool requireGazeToInteract = false;

    [Header("Efek Transisi")]
    [SerializeField] private float fadeDuration = 0.3f; // Durasi fade dalam detik
    
    // ─── State ───
    private VRWalkController playerController;
    private Transform playerTransform;
    private bool isPosterOpen = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    // ─── Debug (tampil di Inspector saat Play) ───
    [Header("Debug (Read Only)")]
    [SerializeField] private bool dbg_isNearby = false;
    [SerializeField] private bool dbg_isGazing = false;
    [SerializeField] private float dbg_distance = 0f;

    void Start()
    {
        // Cari VRWalkController di scene
        playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            Debug.Log("[" + posterTitle + "] ✓ Player ditemukan: " + playerTransform.name);
        }
        else
        {
            Debug.LogError("[" + posterTitle + "] ✗ VRWalkController tidak ditemukan di scene!");
        }

        // Validasi Box Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError("[" + posterTitle + "] ✗ Tidak ada Collider! Tambahkan Box Collider.");
        else
            Debug.Log("[" + posterTitle + "] ✓ Collider ditemukan. Is Trigger: " + col.isTrigger);

        // Validasi Poster Panel & Setup CanvasGroup untuk Fade
        if (posterPanel == null)
        {
            Debug.LogError("[" + posterTitle + "] ✗ Poster Panel belum di-assign di Inspector!");
        }
        else
        {
            // Ambil atau tambahkan CanvasGroup secara otomatis agar bisa fade
            canvasGroup = posterPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = posterPanel.AddComponent<CanvasGroup>();
            }
            
            // Set ke 0 di awal dan nonaktifkan
            canvasGroup.alpha = 0f;
            posterPanel.SetActive(false); 
            Debug.Log("[" + posterTitle + "] ✓ Poster Panel & CanvasGroup Siap.");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // ─── Hitung jarak player ke poster ───
        dbg_distance = Vector3.Distance(transform.position, playerTransform.position);
        dbg_isNearby = (dbg_distance <= interactionDistance);

        // ─── Cek Gaze (raycast dari kamera) ───
        dbg_isGazing = CheckGaze();

        // ─── Kondisi bisa interact ───
        bool canInteract = dbg_isNearby && (!requireGazeToInteract || dbg_isGazing);

        // ─── Tekan E → Buka Poster ───
        if (canInteract && Input.GetKeyDown(KeyCode.E) && !isPosterOpen)
        {
            OpenPoster();
        }

        // ─── Tekan Q → Tutup Poster ───
        if (isPosterOpen && Input.GetKeyDown(KeyCode.Q))
        {
            ClosePoster();
        }

        // ─── Auto tutup jika player terlalu jauh ───
        if (isPosterOpen && !dbg_isNearby)
        {
            ClosePoster();
        }
    }

    /// <summary>
    /// Raycast dari center kamera, return true jika mengenai poster ini
    /// </summary>
    private bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance + 2f))
        {
            // Cek object ini atau child-nya
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Aktifkan panel UI poster & jalankan Fade In
    /// </summary>
    private void OpenPoster()
    {
        if (posterPanel == null || canvasGroup == null)
        {
            Debug.LogError("[" + posterTitle + "] ✗ UI Panel atau CanvasGroup NULL!");
            return;
        }

        isPosterOpen = true;

        // Kunci gerakan player
        if (playerController != null)
            playerController.LockMovement();

        // Hentikan fade yang sedang berjalan (jika ada)
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Aktifkan GameObject & mulai Fade In
        posterPanel.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f));

        Debug.Log("[" + posterTitle + "] ✓ MEMBUKA POSTER (Fade In) → Tekan Q untuk tutup");
    }

    /// <summary>
    /// Jalankan Fade Out & nonaktifkan panel UI poster
    /// </summary>
    private void ClosePoster()
    {
        if (posterPanel == null || canvasGroup == null) return;

        isPosterOpen = false;

        // Buka kembali gerakan player
        if (playerController != null)
            playerController.UnlockMovement();

        // Hentikan fade yang sedang berjalan (jika ada)
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Mulai Fade Out, lalu SetActive(false) setelah selesai
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, () => {
            posterPanel.SetActive(false);
        }));

        Debug.Log("[" + posterTitle + "] ✓ MENUTUP POSTER (Fade Out)");
    }

    /// <summary>
    /// Coroutine untuk mengubah alpha secara smooth (Fade)
    /// </summary>
    private IEnumerator FadeCanvas(float startAlpha, float targetAlpha, System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // ─── Trigger fallback (backup jika collider pakai Is Trigger) ───
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") ||
            other.GetComponent<VRWalkController>() != null ||
            other.GetComponentInParent<VRWalkController>() != null)
        {
            Debug.Log("[" + posterTitle + "] Trigger: Player mendekat → Tekan E untuk melihat");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") ||
            other.GetComponent<VRWalkController>() != null ||
            other.GetComponentInParent<VRWalkController>() != null)
        {
            if (isPosterOpen) ClosePoster();
        }
    }

    // ─── Gizmo di Scene View ───
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
