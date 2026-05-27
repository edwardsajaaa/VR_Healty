using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script Interaksi Poster
/// - Pasang script ini pada GameObject Picture/Poster (yang sudah punya Box Collider)
/// - Isi field "Poster Panel" dengan GameObject Image/Panel UI yang ingin ditampilkan
/// - Tekan E saat dekat poster → panel aktif
/// - Tekan Q untuk menutup panel
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

    // ─── State ───
    private VRWalkController playerController;
    private Transform playerTransform;
    private bool isPosterOpen = false;

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

        // Validasi Poster Panel
        if (posterPanel == null)
            Debug.LogError("[" + posterTitle + "] ✗ Poster Panel belum di-assign di Inspector!");
        else
        {
            posterPanel.SetActive(false); // Sembunyikan di awal
            Debug.Log("[" + posterTitle + "] ✓ Poster Panel: " + posterPanel.name);
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
    /// Aktifkan panel UI poster & kunci pergerakan player
    /// </summary>
    private void OpenPoster()
    {
        if (posterPanel == null)
        {
            Debug.LogError("[" + posterTitle + "] ✗ Poster Panel NULL! Assign dulu di Inspector.");
            return;
        }

        isPosterOpen = true;
        posterPanel.SetActive(true);

        if (playerController != null)
            playerController.LockMovement();

        Debug.Log("[" + posterTitle + "] ✓ POSTER DIBUKA → Tekan Q untuk tutup");
    }

    /// <summary>
    /// Nonaktifkan panel UI poster & buka kembali pergerakan player
    /// </summary>
    private void ClosePoster()
    {
        isPosterOpen = false;

        if (posterPanel != null)
            posterPanel.SetActive(false);

        if (playerController != null)
            playerController.UnlockMovement();

        Debug.Log("[" + posterTitle + "] ✓ POSTER DITUTUP");
    }

    // ─── Trigger fallback (backup jika collider pakai Is Trigger) ───
    private void OnTriggerEnter(Collider other)
    {
        // Deteksi player via tag atau komponen (termasuk parent)
        if (other.CompareTag("Player") ||
            other.GetComponent<VRWalkController>() != null ||
            other.GetComponentInParent<VRWalkController>() != null)
        {
            Debug.Log("[" + posterTitle + "] Trigger: Player masuk area → Tekan E untuk lihat poster");
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
