using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tambahkan script ini pada setiap button di Main Menu (Play, About, Quit).
/// Fitur:
/// - Saat player mengarahkan cursor (gaze) ke button → animasi hover (scale up + highlight)
/// - Tap layar saat melihat button → trigger onClick button
/// - Kompatibel dengan CardboardReticlePointer & touch screen Android
/// </summary>
[RequireComponent(typeof(Button))]
public class MainMenuGazeButton : MonoBehaviour
{
    [Header("Pengaturan Hover")]
    [Tooltip("Warna button saat normal")]
    public Color normalColor = Color.white;

    [Tooltip("Warna button saat di-gaze / hover")]
    public Color hoverColor = new Color(0.75f, 0.92f, 1f);

    [Tooltip("Warna button saat diklik / ditekan")]
    public Color pressedColor = new Color(0.5f, 0.82f, 1f);

    [Tooltip("Skala button saat di-hover (1 = normal, 1.08 = sedikit membesar)")]
    [Range(1f, 1.3f)]
    public float hoverScale = 1.08f;

    [Tooltip("Durasi animasi transisi (detik)")]
    public float animDuration = 0.15f;

    [Header("Gaze Timer (Opsional)")]
    [Tooltip("Jika true, button akan aktif otomatis setelah player menatap selama gazeTime detik tanpa perlu tap")]
    public bool useGazeTimer = false;

    [Range(0.5f, 5f)]
    public float gazeTime = 2f;

    [Tooltip("Fill image untuk progress lingkaran (opsional, assign jika ada)")]
    public Image gazeProgressImage;

    // ─── State ────────────────────────────────────────────────────────────────
    private bool isGazing = false;
    private float gazeTimer = 0f;
    private bool hasTriggeredThisGaze = false; // cegah double trigger

    // ─── Komponen ─────────────────────────────────────────────────────────────
    private Button button;
    private Image buttonImage;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private Coroutine colorCoroutine;
    private Camera mainCamera;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;
        mainCamera = Camera.main;

        // Pastikan collider ada supaya raycast bisa mendeteksi button ini
        if (GetComponent<Collider>() == null)
        {
            // Tambah Box Collider otomatis sesuai ukuran RectTransform
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
                col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
        }

        // Inisialisasi warna normal
        if (buttonImage != null)
            buttonImage.color = normalColor;

        // Sembunyikan progress image jika ada
        if (gazeProgressImage != null)
            gazeProgressImage.fillAmount = 0f;
    }

    void Update()
    {
        if (mainCamera == null) { mainCamera = Camera.main; return; }

        bool currentlyGazing = CheckGaze();

        // Transisi masuk hover
        if (currentlyGazing && !isGazing)
        {
            isGazing = true;
            hasTriggeredThisGaze = false;
            gazeTimer = 0f;
            OnGazeEnter();
        }
        // Transisi keluar hover
        else if (!currentlyGazing && isGazing)
        {
            isGazing = false;
            gazeTimer = 0f;
            OnGazeExit();
        }

        // Update saat sedang di-gaze
        if (isGazing && !hasTriggeredThisGaze)
        {
            // ── Gaze Timer ──
            if (useGazeTimer)
            {
                gazeTimer += Time.deltaTime;
                if (gazeProgressImage != null)
                    gazeProgressImage.fillAmount = Mathf.Clamp01(gazeTimer / gazeTime);

                if (gazeTimer >= gazeTime)
                {
                    TriggerButton();
                    return;
                }
            }

            // ── Tap Layar ──
            if (DetectTap())
                TriggerButton();
        }
    }

    // ─── Gaze Detection ───────────────────────────────────────────────────────

    private bool CheckGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // Jarak cek cukup jauh supaya terdeteksi dari posisi player berdiri
        if (Physics.Raycast(ray, out hit, 15f))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    // ─── Tap Detection ────────────────────────────────────────────────────────

    private bool DetectTap()
    {
        // Android touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // Abaikan jika tap mengenai UI element lain
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return false;
                return true;
            }
        }

        // Editor fallback: klik kiri
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        return false;
    }

    // ─── Hover Effects ────────────────────────────────────────────────────────

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

    // ─── Trigger ──────────────────────────────────────────────────────────────

    private void TriggerButton()
    {
        if (!button.interactable) return;

        hasTriggeredThisGaze = true;
        StartCoroutine(PressEffect());
    }

    private IEnumerator PressEffect()
    {
        // Animasi tekan (scale kecil + warna pressed)
        AnimateScale(originalScale * 0.94f, 0.07f);
        AnimateColor(pressedColor, 0.07f);

        yield return new WaitForSeconds(0.12f);

        // Kembali ke hover
        AnimateScale(originalScale * hoverScale, 0.07f);
        AnimateColor(hoverColor, 0.07f);

        yield return new WaitForSeconds(0.1f);

        // Invoke onClick
        button.onClick.Invoke();
    }

    // ─── Animation Helpers ────────────────────────────────────────────────────

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

    // ─── Reset saat disabled ──────────────────────────────────────────────────

    void OnDisable()
    {
        transform.localScale = originalScale;
        if (buttonImage != null) buttonImage.color = normalColor;
        if (gazeProgressImage != null) gazeProgressImage.fillAmount = 0f;
        isGazing = false;
        gazeTimer = 0f;
    }
}
