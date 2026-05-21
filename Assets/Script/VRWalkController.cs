using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VRWalkController : MonoBehaviour
{
    [Header("Pengaturan Jalan")]
    public float speed = 3.0f;          // Kecepatan jalan
    public float walkThreshold = 20.0f; // Sudut minimal mendongak untuk mulai jalan
    public bool isWalking = false;
    
    [Header("Lock Movement")]
    private bool isMovementLocked = false;

    private Transform camTransform;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        // Jika movement dikunci, jangan gerak
        if (isMovementLocked)
        {
            isWalking = false;
            return;
        }
        
        // 1. Ambil sudut rotasi X kamera
        float headPitch = camTransform.eulerAngles.x;

        // 2. LOGIKA BARU: Cek apakah pemain MENDONGAK (Lihat Atas)
        // Di Unity, mendongak itu angkanya 360 turun ke 270.
        // Jika threshold 20, maka batasnya adalah 360 - 20 = 340.
        // Jadi kalau sudut kepala < 340 (misal 330, 300), berarti sedang mendongak dalam.
        // Kita tambah batas > 270 supaya tidak kebablasan sampai kayang ke belakang.
        
        if (headPitch < (360.0f - walkThreshold) && headPitch > 270.0f)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

        // 3. Eksekusi Gerakan
        if (isWalking)
        {
            MoveForward();
        }
    }

    void MoveForward()
    {
        Vector3 forward = camTransform.forward;
        forward.y = 0;
        forward.Normalize(); 
        controller.SimpleMove(forward * speed);
    }
    
    /// <summary>
    /// Mengunci pergerakan player (digunakan saat melihat poster)
    /// </summary>
    public void LockMovement()
    {
        isMovementLocked = true;
        isWalking = false;
    }
    
    /// <summary>
    /// Membuka kunci pergerakan player
    /// </summary>
    public void UnlockMovement()
    {
        isMovementLocked = false;
    }
}