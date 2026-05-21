using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPoster : MonoBehaviour
{
    [Header("Poster Settings")]
    [SerializeField] private Sprite posterImage;
    [SerializeField] private string posterTitle = "Poster";
    
    [Header("Interaksi")]
    [SerializeField] private float interactionDistance = 3.0f;
    
    [Header("UI Feedback")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material normalMaterial;
    
    private Transform playerTransform;
    private VRWalkController playerController;
    private bool isPlayerNearby = false;
    private bool isPosterOpen = false;
    private bool isPointerOnPoster = false;
    
    // UI untuk menampilkan poster
    private Canvas posterCanvas;
    private Image posterImageComponent;
    private bool uiInitialized = false;
    
    // Renderer untuk highlight
    private Renderer posterRenderer;
    private Material[] originalMaterials;

    void Start()
    {
        // Dapatkan player transform dan controller
        playerTransform = FindObjectOfType<VRWalkController>()?.transform;
        playerController = FindObjectOfType<VRWalkController>();
        
        Debug.Log("[InteractionPoster] Setup poster: " + posterTitle);
        
        if (playerTransform == null)
        {
            Debug.LogError("[InteractionPoster] VRWalkController tidak ditemukan di scene!");
        }
        else
        {
            Debug.Log("[InteractionPoster] Player ditemukan: " + playerTransform.name);
        }
        
        // Setup collider trigger 3D
        Collider col3D = GetComponent<Collider>();
        if (col3D == null)
        {
            Debug.LogError("[InteractionPoster] Poster harus memiliki Collider 3D! Tambahkan Box Collider atau Sphere Collider.");
        }
        else
        {
            if (!col3D.isTrigger)
            {
                Debug.LogWarning("[InteractionPoster] Collider belum di-set sebagai Trigger! Set 'Is Trigger' = true");
            }
            else
            {
                Debug.Log("[InteractionPoster] Collider 3D Trigger sudah aktif ✓");
            }
        }
        
        // Get renderer untuk highlight
        posterRenderer = GetComponent<Renderer>();
        if (posterRenderer != null)
        {
            originalMaterials = posterRenderer.materials;
            Debug.Log("[InteractionPoster] Renderer ditemukan dengan " + originalMaterials.Length + " material");
        }
        
        // Cek tag player
        if (!string.IsNullOrEmpty(gameObject.tag))
        {
            Debug.Log("[InteractionPoster] Tag poster: " + gameObject.tag);
        }
    }

    void Update()
    {
        // Cek raycast dari kursor
        CheckCursorPointer();
        
        if (!isPlayerNearby || playerTransform == null) return;
        
        // Cek apakah player masih dekat
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > interactionDistance)
        {
            isPlayerNearby = false;
            isPointerOnPoster = false;
            Debug.Log("[InteractionPoster] Player terlalu jauh dari " + posterTitle + " (jarak: " + distance.ToString("F2") + "m)");
            return;
        }
        
        // Tekan E untuk membuka poster
        if (Input.GetKeyDown(KeyCode.E) && !isPosterOpen)
        {
            Debug.Log("[InteractionPoster] Tombol E ditekan!");
            OpenPoster();
        }
        
        // Tekan Q untuk menutup poster
        if (Input.GetKeyDown(KeyCode.Q) && isPosterOpen)
        {
            Debug.Log("[InteractionPoster] Tombol Q ditekan!");
            ClosePoster();
        }
    }

    /// <summary>
    /// Cek apakah crosshair (titik putih di tengah) menunjuk ke poster menggunakan raycast dari center screen
    /// </summary>
    private void CheckCursorPointer()
    {
        // Raycast dari center screen (posisi crosshair)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        bool hitThisPoster = false;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                hitThisPoster = true;
            }
        }
        
        // Ubah highlight state
        if (hitThisPoster != isPointerOnPoster)
        {
            isPointerOnPoster = hitThisPoster;
            
            if (isPointerOnPoster)
            {
                HighlightPoster();
                Debug.Log("[InteractionPoster] ✓ Crosshair menunjuk poster: " + posterTitle + " | Klik/Tekan E untuk membuka");
            }
            else
            {
                UnhighlightPoster();
            }
        }
        
        // Klik mouse atau tombol untuk membuka poster
        if (isPointerOnPoster && Input.GetMouseButtonDown(0) && !isPosterOpen)
        {
            Debug.Log("[InteractionPoster] Klik terdeteksi!");
            OpenPoster();
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<VRWalkController>() != null)
        {
            isPlayerNearby = true;
            Debug.Log("[InteractionPoster] ✓ Player mendekat ke poster: " + posterTitle);
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<VRWalkController>() != null)
        {
            isPlayerNearby = false;
            isPointerOnPoster = false;
            // Jika poster sedang dibuka, tutup otomatis
            if (isPosterOpen)
            {
                ClosePoster();
            }
            UnhighlightPoster();
            Debug.Log("[InteractionPoster] Player pergi dari poster: " + posterTitle);
        }
    }

    private void OpenPoster()
    {
        isPosterOpen = true;
        Debug.Log("[InteractionPoster] === MEMBUKA POSTER: " + posterTitle + " ===");
        
        // Inisialisasi UI jika belum
        if (!uiInitialized)
        {
            Debug.Log("[InteractionPoster] Inisialisasi UI...");
            InitializePosterUI();
        }
        
        // Tampilkan poster
        if (posterCanvas != null)
        {
            posterCanvas.gameObject.SetActive(true);
            Debug.Log("[InteractionPoster] ✓ Canvas ditampilkan");
        }
        else
        {
            Debug.LogError("[InteractionPoster] posterCanvas adalah NULL!");
        }
        
        // Kunci pergerakan player
        if (playerController != null)
        {
            playerController.LockMovement();
            Debug.Log("[InteractionPoster] ✓ Movement player terkunci");
        }
        else
        {
            Debug.LogError("[InteractionPoster] playerController adalah NULL!");
        }
        
        Debug.Log("[InteractionPoster] === POSTER TERBUKA ===");
    }

    private void ClosePoster()
    {
        isPosterOpen = false;
        Debug.Log("[InteractionPoster] === MENUTUP POSTER: " + posterTitle + " ===");
        
        // Sembunyikan poster
        if (posterCanvas != null)
        {
            posterCanvas.gameObject.SetActive(false);
            Debug.Log("[InteractionPoster] ✓ Canvas disembunyikan");
        }
        
        // Buka kembali pergerakan player
        if (playerController != null)
        {
            playerController.UnlockMovement();
            Debug.Log("[InteractionPoster] ✓ Movement player dibuka");
        }
        
        Debug.Log("[InteractionPoster] === POSTER TERTUTUP ===");
    }

    private void HighlightPoster()
    {
        if (posterRenderer != null && highlightMaterial != null)
        {
            posterRenderer.material = highlightMaterial;
        }
    }

    private void UnhighlightPoster()
    {
        if (posterRenderer != null && originalMaterials != null && originalMaterials.Length > 0)
        {
            posterRenderer.materials = originalMaterials;
        }
    }

    private void InitializePosterUI()
    {
        Debug.Log("[InteractionPoster] Mulai inisialisasi UI...");
        
        // Cari atau buat Canvas untuk poster
        posterCanvas = FindObjectOfType<Canvas>();
        
        if (posterCanvas == null)
        {
            Debug.Log("[InteractionPoster] Canvas tidak ditemukan, membuat Canvas baru...");
            // Buat Canvas baru jika tidak ada
            GameObject canvasObj = new GameObject("PosterCanvas");
            posterCanvas = canvasObj.AddComponent<Canvas>();
            posterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }
        else
        {
            Debug.Log("[InteractionPoster] Canvas ditemukan: " + posterCanvas.name);
        }
        
        // Cari atau buat Image component untuk menampilkan poster
        posterImageComponent = posterCanvas.GetComponentInChildren<Image>();
        
        if (posterImageComponent == null)
        {
            Debug.Log("[InteractionPoster] Image component tidak ditemukan, membuat Image baru...");
            // Buat Image baru
            GameObject imageObj = new GameObject("PosterImage");
            imageObj.transform.SetParent(posterCanvas.transform, false);
            
            posterImageComponent = imageObj.AddComponent<Image>();
            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            Debug.Log("[InteractionPoster] ✓ Image baru dibuat dan di-set fullscreen");
        }
        else
        {
            Debug.Log("[InteractionPoster] Image component ditemukan");
        }
        
        // Set gambar poster
        if (posterImage != null)
        {
            posterImageComponent.sprite = posterImage;
            Debug.Log("[InteractionPoster] ✓ Sprite assigned: " + posterImage.name);
        }
        else
        {
            Debug.LogWarning("[InteractionPoster] ⚠️ posterImage adalah NULL! Assign gambar di Inspector!");
        }
        
        // Sembunyikan canvas di awal
        posterCanvas.gameObject.SetActive(false);
        
        uiInitialized = true;
        Debug.Log("[InteractionPoster] ✓ UI initialization selesai");
    }
}
