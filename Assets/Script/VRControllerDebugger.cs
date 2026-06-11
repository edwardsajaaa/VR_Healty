using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VRControllerDebugger : MonoBehaviour
{
    private Text debugText;
    private string detectedMode = "TUNGGU INPUT (Tekan tombol/gerakkan analog)";
    private Color modeColor = Color.yellow;
    private string lastPressedButtons = "";

    void Start()
    {
        // Setup Canvas UI Transparan
        GameObject canvasObj = new GameObject("VR_InputDebugCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(canvasObj.transform, false);
        debugText = textObj.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 35;
        debugText.alignment = TextAnchor.UpperLeft;
        
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(40, 40);
        rt.offsetMax = new Vector2(-40, -40);
        
        Image bg = textObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

        bool joystickDetected = false;
        bool wrongModeDetected = false;
        string currentKeys = "";

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            h = stick.x;
            v = stick.y;

            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                {
                    currentKeys += "-> Gamepad: " + control.name + "\n";
                    joystickDetected = true;
                }
            }
        }

        // Jika terdeteksi input keyboard/mouse di Android, biasanya itu mode Media/Mouse
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
        {
            wrongModeDetected = true;
            foreach (var control in Keyboard.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                {
                    currentKeys += "-> Keyboard: " + control.name + "\n";
                }
            }
        }

        if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
        {
            wrongModeDetected = true;
            currentKeys += "-> Mouse Click\n";
        }

        if (currentKeys != "") lastPressedButtons = currentKeys;

        // Cek pergerakan analog (nilai desimal selain 0 dan 1)
        bool analogMoving = Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f;

        // Logika penentuan status mode controller
        if (wrongModeDetected)
        {
            detectedMode = "❌ MODE SALAH (Mouse/Media Mode)\nSolusi: Tahan [@] lalu tekan [B]";
            modeColor = new Color(1f, 0.3f, 0.3f); // Merah
        }
        else if (joystickDetected || analogMoving)
        {
            detectedMode = "✅ GAME MODE AKTIF (Siap Digunakan!)";
            modeColor = new Color(0.3f, 1f, 0.3f); // Hijau
        }

        // Render Teks ke Layar
        string info = "<b>=== CEK MODE CONTROLLER VR ===</b>\n\n";
        
        info += "<color=#" + ColorUtility.ToHtmlStringRGB(modeColor) + "><b>STATUS: " + detectedMode + "</b></color>\n\n";
        
        info += "Analog Kiri/Kanan : " + h.ToString("F2") + "\n";
        info += "Analog Atas/Bawah : " + v.ToString("F2") + "\n\n";
        
        info += "Tombol Terakhir:\n" + (lastPressedButtons == "" ? "(Belum ada)" : lastPressedButtons);

        debugText.text = info;
    }
}
