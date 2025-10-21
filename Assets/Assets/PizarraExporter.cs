using UnityEngine;
using System.IO;

public class PizarraExporter : MonoBehaviour
{
    [Tooltip("Asigna la Main Camera (la que está dentro del XR Origin)")]
    public Camera mainCamera;

    [Tooltip("Resolución de la imagen")]
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;

    // Tecla de prueba para editor
    public KeyCode testKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(testKey))
            CaptureScreenshot();
    }

    public void CaptureScreenshot()
    {
        if (mainCamera == null)
        {
            Debug.LogError("ScreenshotCapture: no está asignada la Main Camera.");
            return;
        }

        // Crear RenderTexture temporal
        RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        mainCamera.targetTexture = rt;

        // Forzar renderizado de la cámara
        mainCamera.Render();

        // Leer los pixeles del RenderTexture
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
        screenshot.Apply();

        // Restaurar estado de la cámara
        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Guardar en disco
        string filename = $"BoardScreenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string folder = Application.persistentDataPath; // cross-platform
        string path = Path.Combine(folder, filename);
        File.WriteAllBytes(path, screenshot.EncodeToPNG());

        Debug.Log($"📸 Screenshot guardada en: {path}");
    }
}
