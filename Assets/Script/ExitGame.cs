using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Fungsi ini akan dipanggil ketika Button di-klik
    public void QuitGame()
    {
        Debug.Log("Keluar dari game...");

        #if UNITY_EDITOR
        // Jika sedang berjalan di Unity Editor, hentikan Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // Jika sudah di-build, tutup aplikasi
        Application.Quit();
        #endif
    }
}
