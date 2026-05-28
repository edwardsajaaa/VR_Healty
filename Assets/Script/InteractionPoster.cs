using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPoster : MonoBehaviour
{
    [Header("Poster Settings")]
    [SerializeField] private string posterTitle = "Poster";

    [Header("Panel UI")]
    [SerializeField] private GameObject posterPanel;

    [Header("Interaksi")]
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private bool requireGazeToInteract = false;

    [Header("Transisi")]
    [SerializeField] private float transitionDuration = 0.35f;

    private VRWalkController playerController;
    private Transform playerTransform;
    private bool isPosterOpen = false;
    private bool isTransitioning = false;

    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Coroutine transitionCoroutine;

    [Header("Debug (Read Only)")]
    [SerializeField] private bool dbg_isNearby = false;
    [SerializeField] private bool dbg_isGazing = false;
    [SerializeField] private float dbg_distance = 0f;

    void Start()
    {
        playerController = FindObjectOfType<VRWalkController>();
        if (playerController != null)
            playerTransform = playerController.transform;

        if (posterPanel != null)
        {
            canvasGroup = posterPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = posterPanel.AddComponent<CanvasGroup>();

            panelRect = posterPanel.GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            if (panelRect != null) panelRect.localScale = Vector3.zero;
            posterPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        dbg_distance = Vector3.Distance(transform.position, playerTransform.position);
        dbg_isNearby = (dbg_distance <= interactionDistance);
        dbg_isGazing = CheckGaze();

        bool canInteract = dbg_isNearby && (!requireGazeToInteract || dbg_isGazing);

        if (canInteract && Input.GetKeyDown(KeyCode.E) && !isPosterOpen && !isTransitioning)
            OpenPoster();

        if (isPosterOpen && Input.GetKeyDown(KeyCode.Q) && !isTransitioning)
            ClosePoster();

        if (isPosterOpen && !dbg_isNearby && !isTransitioning)
            ClosePoster();
    }

    private bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance + 2f))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    private void OpenPoster()
    {
        if (posterPanel == null || canvasGroup == null) return;

        isPosterOpen = true;

        if (playerController != null)
            playerController.LockMovement();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        posterPanel.SetActive(true);
        transitionCoroutine = StartCoroutine(TransitionOpen());
    }

    private void ClosePoster()
    {
        if (posterPanel == null || canvasGroup == null) return;

        isPosterOpen = false;

        if (playerController != null)
            playerController.UnlockMovement();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionClose());
    }

    private IEnumerator TransitionOpen()
    {
        isTransitioning = true;
        float elapsed = 0f;

        canvasGroup.alpha = 0f;
        if (panelRect != null) panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float eased = EaseOutBack(t);

            canvasGroup.alpha = Mathf.Clamp01(t / 0.6f);

            if (panelRect != null)
            {
                float scale = Mathf.LerpUnclamped(0.85f, 1f, eased);
                panelRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (panelRect != null) panelRect.localScale = Vector3.one;
        isTransitioning = false;
    }

    private IEnumerator TransitionClose()
    {
        isTransitioning = true;
        float elapsed = 0f;

        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = panelRect != null ? panelRect.localScale : Vector3.one;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float eased = EaseInCubic(t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            if (panelRect != null)
            {
                float scale = Mathf.Lerp(startScale.x, 0.85f, eased);
                panelRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (panelRect != null) panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);
        posterPanel.SetActive(false);
        isTransitioning = false;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") ||
            other.GetComponent<VRWalkController>() != null ||
            other.GetComponentInParent<VRWalkController>() != null)
        {
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
