using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseLook : MonoBehaviour
{
    public float sensitivity = 200f;

    void Update()
    {
        // Script ini jalan di Unity Editor & PC Desktop Laptop (otomatis mati saat di headset VR/HP)
        if (!Application.isEditor && SystemInfo.deviceType != DeviceType.Desktop) return;

        bool rightClick = false;
        bool altKey = false;
        Vector2 delta = Vector2.zero;

        if (Mouse.current != null)
        {
            rightClick = Mouse.current.rightButton.isPressed;
            delta = Mouse.current.delta.ReadValue();
        }
        if (Keyboard.current != null)
        {
            altKey = Keyboard.current.leftAltKey.isPressed;
        }

        // Fallback ke Legacy Input System jika New Input System nilainya 0 atau tidak aktif
        if (!rightClick && !altKey)
        {
            try
            {
                if (Input.GetMouseButton(1)) rightClick = true;
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) altKey = true;
            }
            catch { }
        }

        if (delta == Vector2.zero && (rightClick || altKey))
        {
            try
            {
                delta = new Vector2(Input.GetAxisRaw("Mouse X") * 10f, Input.GetAxisRaw("Mouse Y") * 10f);
            }
            catch { }
        }

        // Tahan Klik Kanan (Mouse 1) atau Alt untuk menengok
        if (rightClick || altKey)
        {
            // New Input System delta nilainya bisa sangat besar, jadi kita kurangi sensitivitasnya (dibagi 10)
            float mouseX = delta.x * (sensitivity / 10f) * Time.deltaTime;
            float mouseY = delta.y * (sensitivity / 10f) * Time.deltaTime;

            // Putar player/kamera
            // Rotasi Y (Kiri Kanan): Jika dipasang di Kamera, putar Parent (Player) agar arah depan pergerakan jalan ikut berputar
            if (transform.parent != null)
            {
                transform.parent.Rotate(Vector3.up * mouseX);
            }
            else
            {
                transform.Rotate(Vector3.up * mouseX);
            }
            
            // Rotasi X (Atas Bawah)
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.x -= mouseY;
            transform.localEulerAngles = currentRotation;
        }
    }
}