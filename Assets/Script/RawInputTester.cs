using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// Script independen untuk mengecek sinyal mentah (Raw Input) dari controller Bluetooth.
/// Ditampilkan dalam bentuk UI 3D melayang (World Space) seperti Poster agar nyaman dibaca di VR.
/// </summary>
public class RawInputTester : MonoBehaviour
{
    public float distanceDidepanKamera = 3.0f;
    public float tinggiPanel = 0.5f;

    private Canvas setupCanvas;
    private Text logTextUI;
    
    private string logText = "Mendeteksi Input...\n";
    private List<string> activeKeys = new List<string>();
    private Array allKeyCodes;

    void Start()
    {
        // Ambil semua daftar tombol yang ada di Unity
        allKeyCodes = Enum.GetValues(typeof(KeyCode));
        
        Create3DPanel();
    }

    private void Create3DPanel()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("RawInputTester: Main Camera tidak ditemukan!");
            return;
        }

        // 1. Buat Canvas Object
        GameObject canvasObj = new GameObject("Raw_Input_Canvas_3D");
        setupCanvas = canvasObj.AddComponent<Canvas>();
        setupCanvas.renderMode = RenderMode.WorldSpace;
        
        // Jadikan canvas sebagai child dari kamera agar selalu mengikuti pergerakan dan rotasi player
        canvasObj.transform.SetParent(cam.transform);
        canvasObj.transform.localPosition = new Vector3(0, tinggiPanel, distanceDidepanKamera);
        canvasObj.transform.localRotation = Quaternion.identity;
        
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(900, 700); // Ukuran virtual
        canvasRT.localScale = new Vector3(0.005f, 0.005f, 0.005f); // Skala di dunia 3D

        // 2. Buat Background Panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // 3. Buat Text UI
        GameObject txtObj = new GameObject("LogText");
        txtObj.transform.SetParent(canvasObj.transform, false);
        logTextUI = txtObj.AddComponent<Text>();
        logTextUI.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logTextUI.fontSize = 28;
        logTextUI.color = Color.white;
        logTextUI.alignment = TextAnchor.UpperLeft;
        logTextUI.supportRichText = true;
        
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0.05f, 0.05f);
        txtRT.anchorMax = new Vector2(0.95f, 0.95f);
        txtRT.sizeDelta = Vector2.zero;
        txtRT.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (logTextUI == null) return;

        activeKeys.Clear();

        // 1. CEK SEMUA TOMBOL (A-Z, Panah, Joystick 0-19, dll)
        foreach (KeyCode k in allKeyCodes)
        {
            if (Input.GetKey(k))
            {
                activeKeys.Add(k.ToString());
            }
        }

        // 2. CEK MOUSE / TOUCH
        if (Input.GetMouseButton(0)) activeKeys.Add("Mouse Kiri / Layar Disentuh");
        if (Input.GetMouseButton(1)) activeKeys.Add("Mouse Kanan");
        if (Input.GetMouseButton(2)) activeKeys.Add("Mouse Tengah");
        
        if (Input.touchCount > 0) activeKeys.Add("Touch Count: " + Input.touchCount);

        // 3. CEK AXIS MENTAH (Analog Stick)
        float h = 0;
        float v = 0;
        float mx = 0;
        float my = 0;
        
        try
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            mx = Input.GetAxisRaw("Mouse X");
            my = Input.GetAxisRaw("Mouse Y");
        }
        catch 
        { 
            // Abaikan error jika Axis belum diset di Project Settings
        }

        // 4. SUSUN TEKS LOG
        logText = "<b><size=40>=== RAW INPUT TESTER ===</size></b>\n";
        logText += "Gunakan panel 3D ini untuk melihat sinyal asli dari remote Anda.\n\n";

        if (activeKeys.Count > 0)
        {
            logText += "<color=lime><b>TOMBOL DITEKAN (TERBACA):</b></color>\n";
            foreach (string k in activeKeys)
            {
                logText += " ➡ [" + k + "]\n";
            }
        }
        else
        {
            logText += "<color=grey>TOMBOL DITEKAN:\n ➡ (Tidak ada sinyal)</color>\n";
        }

        logText += "\n<color=yellow><b>AXIS (ANALOG / MOUSE):</b></color>\n";
        logText += "Horizontal : " + h.ToString("F2") + "\n";
        logText += "Vertical   : " + v.ToString("F2") + "\n";
        logText += "Mouse X    : " + mx.ToString("F2") + "\n";
        logText += "Mouse Y    : " + my.ToString("F2") + "\n";

        logText += "\n\n<color=#ff8888><i>* Jika Anda dorong analog/tekan tombol C tapi layar ini TIDAK BERUBAH\n";
        logText += "(dan malah memunculkan volume HP), berarti FIX 100% remote Anda\n";
        logText += "sedang tersangkut di Mode Media/Volume.</i></color>";

        // Update UI
        logTextUI.text = logText;
    }
}
