using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Maneja el XR Origin en la escena
/// Este script va en el XR Origin que está SIEMPRE en la escena
/// </summary>
public class XROriginManager : MonoBehaviour
{
    [Header("Referencias")]
    public Camera mainCamera;
    public Transform leftController;
    public Transform rightController;

    [Header("Avatar Visual (para otros jugadores)")]
    public GameObject avatarPrefab; // El avatar que otros verán
    private GameObject myVisualAvatar;

    [Header("Configuración")]
    public bool autoFindReferences = true;
    public Vector3 avatarSpawnOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool isInitialized = false;

    void Start()
    {
        if (autoFindReferences)
        {
            FindReferences();
        }

        // Esperar a que Photon se conecte antes de crear avatar
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            SpawnVisualAvatar();
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("⏳ Esperando conexión a Photon...");
        }

        isInitialized = true;
    }

    void FindReferences()
    {
        // Buscar cámara
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera != null && showDebugInfo)
                Debug.Log($"✅ Cámara encontrada: {mainCamera.name}");
        }

        // Buscar controladores
        ActionBasedController[] controllers = GetComponentsInChildren<ActionBasedController>();
        foreach (var controller in controllers)
        {
            if (controller.name.Contains("Left"))
            {
                leftController = controller.transform;
                if (showDebugInfo)
                    Debug.Log($"✅ Controlador izquierdo: {controller.name}");
            }
            else if (controller.name.Contains("Right"))
            {
                rightController = controller.transform;
                if (showDebugInfo)
                    Debug.Log($"✅ Controlador derecho: {controller.name}");
            }
        }
    }

    /// <summary>
    /// Crea el avatar visual que otros jugadores verán
    /// </summary>
    void SpawnVisualAvatar()
    {
        if (avatarPrefab == null)
        {
            Debug.LogWarning("⚠️ Avatar Prefab no asignado. Otros jugadores no verán tu avatar.");
            return;
        }

        // Posición de spawn (ligeramente ajustada)
        Vector3 spawnPosition = transform.position + avatarSpawnOffset;

        // Instanciar avatar en red
        myVisualAvatar = PhotonNetwork.Instantiate(
            avatarPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (showDebugInfo)
            Debug.Log($"✅ Avatar visual creado: {myVisualAvatar.name}");
    }

    /// <summary>
    /// Llamar cuando Photon se conecta (si no estaba conectado en Start)
    /// </summary>
    public void OnPhotonConnected()
    {
        if (isInitialized && myVisualAvatar == null)
        {
            SpawnVisualAvatar();
        }
    }

    /// <summary>
    /// Obtener la posición de la cámara (útil para otros scripts)
    /// </summary>
    public Vector3 GetCameraPosition()
    {
        return mainCamera != null ? mainCamera.transform.position : transform.position;
    }

    /// <summary>
    /// Obtener la rotación de la cámara
    /// </summary>
    public Quaternion GetCameraRotation()
    {
        return mainCamera != null ? mainCamera.transform.rotation : transform.rotation;
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Dibujar posición de spawn del avatar
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + avatarSpawnOffset, 0.3f);

        // Dibujar dirección frontal
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label("=== XR Origin Status ===");
        GUILayout.Label($"Cámara: {(mainCamera != null ? "✓" : "✗")}");
        GUILayout.Label($"Left Controller: {(leftController != null ? "✓" : "✗")}");
        GUILayout.Label($"Right Controller: {(rightController != null ? "✓" : "✗")}");
        GUILayout.Label($"Avatar Visual: {(myVisualAvatar != null ? "✓" : "✗")}");
        GUILayout.Label($"Photon: {(PhotonNetwork.IsConnected ? "✓" : "✗")}");
        GUILayout.EndArea();
    }
}
