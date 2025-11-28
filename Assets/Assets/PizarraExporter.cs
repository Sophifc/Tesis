using UnityEngine;
using System.IO;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Captura screenshots de la pizarra desde la cámara VR
/// Coloca este script en la Pizarra o en un GameObject de gestión
/// </summary>
public class PizarraExporter : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("La cámara se busca automáticamente. Puedes asignarla manualmente si falla.")]
    public Camera mainCamera;

    [Tooltip("Resolución de la imagen")]
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;

    [Header("Controles")]
    public KeyCode screenshotKey = KeyCode.P; // Cambiado a P para evitar conflicto con E
    public bool allowScreenshotsInBuild = true;

    [Header("Guardado")]
    public string screenshotPrefix = "Pizarra";
    public bool saveToMyPictures = true;
    public string customFolder = ""; // Opcional: ruta personalizada

    [Header("Debug")]
    public bool showDebugMessages = true;

    private bool isCapturing = false;
    private bool cameraFound = false;

    // ====================================================================
    // INICIALIZACIÓN
    // ====================================================================
    void Start()
    {
        FindMainCamera();
    }

    void FindMainCamera()
    {
        // Estrategia 1: Si ya está asignada manualmente
        if (mainCamera != null)
        {
            cameraFound = true;
            if (showDebugMessages)
                Debug.Log($"✅ Cámara asignada manualmente: {mainCamera.name}");
            return;
        }

        // Estrategia 2: Buscar por tag "MainCamera"
        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj != null)
        {
            mainCamera = cameraObj.GetComponent<Camera>();
            if (mainCamera != null)
            {
                cameraFound = true;
                if (showDebugMessages)
                    Debug.Log($"✅ Cámara encontrada por tag: {mainCamera.name}");
                return;
            }
        }

        // Estrategia 3: Buscar Camera.main
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraFound = true;
            if (showDebugMessages)
                Debug.Log($"✅ Cámara encontrada (Camera.main): {mainCamera.name}");
            return;
        }

        // Estrategia 4: Buscar dentro del XR Origin
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            mainCamera = xrOrigin.Camera;
            if (mainCamera != null)
            {
                cameraFound = true;
                if (showDebugMessages)
                    Debug.Log($"✅ Cámara encontrada en XR Origin: {mainCamera.name}");
                return;
            }
        }

        // Estrategia 5: Buscar cualquier cámara activa en la escena
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                mainCamera = cam;
                cameraFound = true;
                if (showDebugMessages)
                    Debug.Log($"✅ Cámara encontrada (búsqueda general): {mainCamera.name}");
                return;
            }
        }

        // Si llegamos aquí, no encontramos ninguna cámara
        Debug.LogError("❌ No se encontró ninguna cámara activa en la escena.");
        Debug.LogError("   Asigna manualmente la cámara en el Inspector del PizarraExporter.");
    }

    // ====================================================================
    // UPDATE - DETECTAR INPUT
    // ====================================================================
    void Update()
    {
        // Solo permitir screenshots en build si está habilitado
#if !UNITY_EDITOR
        if (!allowScreenshotsInBuild)
            return;
#endif

        // Si la cámara no se encontró, intentar de nuevo
        if (!cameraFound)
        {
            FindMainCamera();
            return;
        }

        // Detectar tecla de screenshot
        if (Input.GetKeyDown(screenshotKey))
        {
            CaptureScreenshot();
        }
    }

    // ====================================================================
    // CAPTURA DE SCREENSHOT
    // ====================================================================
    public void CaptureScreenshot()
    {
        if (mainCamera == null)
        {
            Debug.LogError("❌ No se puede capturar screenshot: cámara no encontrada.");
            FindMainCamera(); // Intentar encontrarla de nuevo
            return;
        }

        if (isCapturing)
        {
            Debug.LogWarning("⚠️ Ya se está procesando una captura.");
            return;
        }

        StartCoroutine(CaptureAndSaveCoroutine());
    }

    // ====================================================================
    // CORRUTINA DE CAPTURA Y GUARDADO
    // ====================================================================
    private IEnumerator CaptureAndSaveCoroutine()
    {
        isCapturing = true;

        if (showDebugMessages)
            Debug.Log("📸 Iniciando captura de screenshot...");

        // 1. Configurar el RenderTexture
        RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        RenderTexture previousRT = mainCamera.targetTexture;
        mainCamera.targetTexture = rt;
        RenderTexture.active = rt;

        // 2. Renderizar y leer los píxeles
        mainCamera.Render();
        Texture2D screenshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
        screenshot.Apply();

        // 3. Restaurar la cámara
        mainCamera.targetTexture = previousRT;
        RenderTexture.active = null;
        Destroy(rt);

        if (showDebugMessages)
            Debug.Log("📸 Captura completada, codificando a PNG...");

        // 4. Esperar un frame
        yield return null;

        // 5. Codificar a PNG (proceso lento)
        byte[] pngData = screenshot.EncodeToPNG();
        Destroy(screenshot);

        if (showDebugMessages)
            Debug.Log("📸 Codificación completada, guardando archivo...");

        // 6. Esperar otro frame
        yield return null;

        // 7. Determinar la ruta de guardado
        string folder;
        if (!string.IsNullOrEmpty(customFolder))
        {
            folder = customFolder;
        }
        else if (saveToMyPictures)
        {
            folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
        }
        else
        {
            folder = Application.persistentDataPath;
        }

        // Crear carpeta si no existe
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        // 8. Generar nombre de archivo único
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"{screenshotPrefix}_{timestamp}.png";
        string path = Path.Combine(folder, filename);

        // 9. Guardar archivo
        try
        {
            File.WriteAllBytes(path, pngData);
            Debug.Log($"✅ Screenshot guardado exitosamente:");
            Debug.Log($"   📁 {path}");
            Debug.Log($"   📏 Resolución: {resolutionWidth}x{resolutionHeight}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al guardar screenshot: {e.Message}");
        }

        isCapturing = false;
    }

    // ====================================================================
    // FUNCIONES PÚBLICAS (para botones UI o VR)
    // ====================================================================

    /// <summary>
    /// Captura screenshot con resolución personalizada
    /// </summary>
    public void CaptureScreenshotWithResolution(int width, int height)
    {
        int prevWidth = resolutionWidth;
        int prevHeight = resolutionHeight;

        resolutionWidth = width;
        resolutionHeight = height;

        CaptureScreenshot();

        resolutionWidth = prevWidth;
        resolutionHeight = prevHeight;
    }

    /// <summary>
    /// Captura screenshot de alta resolución (4K)
    /// </summary>
    [ContextMenu("Capturar Screenshot 4K")]
    public void Capture4KScreenshot()
    {
        CaptureScreenshotWithResolution(3840, 2160);
    }

    /// <summary>
    /// Captura screenshot estándar (Full HD)
    /// </summary>
    [ContextMenu("Capturar Screenshot Full HD")]
    public void CaptureFullHDScreenshot()
    {
        CaptureScreenshotWithResolution(1920, 1080);
    }

    // ====================================================================
    // DEBUG
    // ====================================================================
    private void OnGUI()
    {
        if (!showDebugMessages) return;

        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 400, 120));
        GUILayout.Label("=== Screenshot Captura ===");
        GUILayout.Label($"Cámara: {(mainCamera != null ? mainCamera.name : "No encontrada")}");
        GUILayout.Label($"Estado: {(isCapturing ? "Capturando..." : "Listo")}");
        GUILayout.Label($"Tecla: {screenshotKey}");
        GUILayout.EndArea();
    }
}