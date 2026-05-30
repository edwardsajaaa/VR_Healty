using UnityEngine;

/// <summary>
/// Tambahkan script ini pada GameObject yang sama dengan CardboardReticlePointer
/// (atau pada Main Camera).
/// 
/// Fungsi: Membuat cursor/reticle berubah ukuran & warna saat mengarah ke
/// GameObject yang punya komponen MainMenuGazeButton — memberikan feedback
/// visual bahwa element tersebut interaktif.
/// </summary>
public class MainMenuReticleFeedback : MonoBehaviour
{
    [Header("Reticle Visual")]
    [Tooltip("Renderer dari dot/reticle cursor (assign dari CardboardReticlePointer child)")]
    public Renderer reticleRenderer;

    [Tooltip("Warna reticle saat normal")]
    public Color normalColor = Color.white;

    [Tooltip("Warna reticle saat mengarah ke button interaktif")]
    public Color interactColor = new Color(0.3f, 0.85f, 1f);

    [Tooltip("Skala reticle saat hover interaktif")]
    public float hoverReticleScale = 1.8f;

    [Tooltip("Kecepatan transisi")]
    public float transitionSpeed = 8f;

    private Camera mainCamera;
    private bool isOverInteractable = false;
    private float currentScale = 1f;
    private Color currentColor;

    void Start()
    {
        mainCamera = Camera.main;
        if (reticleRenderer != null)
            currentColor = reticleRenderer.material.color;
        else
            currentColor = normalColor;
    }

    void Update()
    {
        if (mainCamera == null) return;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        bool hitInteractable = false;
        if (Physics.Raycast(ray, out hit, 15f))
        {
            if (hit.collider.GetComponent<MainMenuGazeButton>() != null ||
                hit.collider.GetComponentInParent<MainMenuGazeButton>() != null)
                hitInteractable = true;
        }

        isOverInteractable = hitInteractable;

        // Animasi smooth scale & warna
        float targetScale = isOverInteractable ? hoverReticleScale : 1f;
        Color targetColor = isOverInteractable ? interactColor : normalColor;

        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * transitionSpeed);
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * transitionSpeed);

        if (reticleRenderer != null)
        {
            reticleRenderer.transform.localScale = Vector3.one * currentScale;
            reticleRenderer.material.color = currentColor;
        }
    }
}
