using UnityEngine;
using System.IO;
using Photon.Pun;
using System.Collections; // Necesario para la Corutina

[RequireComponent(typeof(PhotonView))] // <-- 2. Asegura que haya un PhotonView
public class PizarraExporter : MonoBehaviour
{
    [Tooltip("Asigna la Main Camera (la que está dentro del XR Origin)")]
    public Camera mainCamera;

    [Tooltip("Resolución de la imagen")]
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;

    public KeyCode testKey = KeyCode.E;

    private bool isCapturing = false; // Para evitar capturas múltiples
    private PhotonView photonView; // <-- 3. AÑADE UNA VARIABLE PARA PHOTON VIEW

    void Awake()
    {
        photonView = GetComponent<PhotonView>(); // <-- 4. OBTÉN LA REFERENCIA

        // 5. LA COMPROBACIÓN MÁGICA
        if (photonView.IsMine)
        {
            // Soy el jugador local. Encuentra mi cámara.
            mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("¡PizarraExporter no pudo encontrar una Cámara en sus hijos!");
            }
        }
        else
        {
            // No soy el jugador local. Desactiva este script por completo.
            // Ya no dará errores ni intentará tomar fotos.
            this.enabled = false;
        }
    }

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

        if (isCapturing)
        {
            Debug.LogWarning("Ya se está procesando una captura.");
            return;
        }

        // En lugar de hacer todo aquí, iniciamos la Corutina
        StartCoroutine(CaptureAndSaveCoroutine());
    }

    private IEnumerator CaptureAndSaveCoroutine()
    {
        isCapturing = true;

        // 1. Configurar el RenderTexture
        RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        mainCamera.targetTexture = rt;
        RenderTexture.active = rt;

        // 2. Renderizar y leer los píxeles (esto debe ser en el hilo principal)
        mainCamera.Render();
        Texture2D screenshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
        screenshot.Apply();

        // 3. Restaurar la cámara (importante hacerlo antes del yield)
        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 4. Esperamos un frame. Esto le da un respiro al motor
        yield return null;

        // 5. Codificar a PNG (lento)
        byte[] pngData = screenshot.EncodeToPNG();
        Destroy(screenshot); // Liberamos la memoria de la textura

        // 6. Guardar en disco (muy lento)
        string filename = $"BoardScreenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        // Esto obtiene la carpeta "Mis Imágenes" en cualquier PC con Windows, Mac o Linux
        string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
        string path = Path.Combine(folder, filename);

        // Para una solución perfecta, esto debería ir en un hilo separado
        // pero File.WriteAllBytesAsync es más complejo.
        // Por ahora, un yield ayuda a que el motor no se ahogue.
        yield return null;

        File.WriteAllBytes(path, pngData);

        Debug.Log($"📸 Screenshot guardada en: {path}");
        isCapturing = false;
    }
}