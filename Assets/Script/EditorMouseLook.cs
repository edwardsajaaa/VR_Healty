using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseLook : MonoBehaviour
{
    public float sensitivity = 200f;

    void Update()
    {
        // Script ini HANYA jalan di Unity Editor (Laptop)
        // Di HP script ini otomatis mati.
        if (!Application.isEditor) return;

        bool rightClick = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool altKey = Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;

        // Tahan Klik Kanan (Mouse 1) atau Alt untuk menengok
        if (rightClick || altKey)
        {
            Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            
            // New Input System delta nilainya bisa sangat besar, jadi kita kurangi sensitivitasnya (dibagi 10)
            float mouseX = delta.x * (sensitivity / 10f) * Time.deltaTime;
            float mouseY = delta.y * (sensitivity / 10f) * Time.deltaTime;

            // Putar player/kamera
            // Rotasi Y (Kiri Kanan)
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotasi X (Atas Bawah) - hati-hati gimbal lock, tapi untuk tes simple ok
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.x -= mouseY;
            transform.localEulerAngles = currentRotation;
        }
    }
}