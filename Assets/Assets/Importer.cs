using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class RequirementLoader : MonoBehaviourPunCallbacks
{
    [Header("Referencias de la Escena")]
    public GameObject postItPrefab; // Debe estar en Resources/PostIt
    public Transform whiteboardParent; // La pizarra principal

    [Header("Avatar")]
    public GameObject playerAvatarPrefab; // Prefab del avatar en Resources/Avatar
    private bool avatarSpawned = false;

    [Header("Configuración de Distribución")]
    public int postItsPerRow = 5;
    public float spacingX = 0.35f;
    public float spacingY = 0.35f;
    public Vector2 startPosition = new Vector2(-0.8f, 0.6f);
    public Vector3 postItScale = new Vector3(0.15f, 0.15f, 0.02f);
    public float postItZOffset = 0.05f;

    [Header("Instanciación")]
    public float delayBetweenPostIts = 0.1f; // ✅ Delay entre cada post-it (100ms)

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool isLoadingPostIts = false; // ✅ Para evitar múltiples cargas simultáneas

    void Start()
    {
        // ✅ YA NO spawneamos avatar aquí - lo hace XROriginSpawner
        // (comentado para evitar duplicados)

        /*
        if (!avatarSpawned)
        {
            SpawnPlayerAvatar();
            avatarSpawned = true;
        }
        */

        // ✅ SOLO el Master Client carga los requerimientos
        if (PhotonNetwork.IsMasterClient)
        {
            if (!string.IsNullOrEmpty(NetworkManager.FilePathToLoad))
            {
                LoadRequirementsFromFile(NetworkManager.FilePathToLoad);
            }
            else
            {
                Debug.LogWarning("⚠️ No se especificó ruta de archivo. No se cargarán requerimientos.");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("Cliente esperando que el Master cargue los requerimientos...");
        }
    }

    /// <summary>
    /// Instancia el avatar VISUAL del jugador (NO el XR Origin)
    /// </summary>
    void SpawnPlayerAvatar()
    {
        if (playerAvatarPrefab == null)
        {
            Debug.LogWarning("⚠️ playerAvatarPrefab no asignado. No se creará avatar visual.");
            return;
        }

        // El avatar visual aparece en el origen, luego seguirá al XR Origin local
        GameObject avatar = PhotonNetwork.Instantiate(
            playerAvatarPrefab.name,
            Vector3.zero,
            Quaternion.identity
        );

        if (showDebugLogs)
            Debug.Log($"✅ Avatar visual de {PhotonNetwork.NickName} creado");
    }

    /// <summary>
    /// Lee el archivo y carga los requerimientos
    /// </summary>
    void LoadRequirementsFromFile(string filePath)
    {
        if (showDebugLogs)
            Debug.Log($"📂 Intentando cargar archivo: {filePath}");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"❌ Archivo no encontrado: {filePath}");
            return;
        }

        try
        {
            string fileContent = File.ReadAllText(filePath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            List<string> validRequirements = new List<string>();

            // Filtrar líneas válidas
            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // Ignorar líneas vacías, comentarios o headers
                if (string.IsNullOrWhiteSpace(trimmed) ||
                    trimmed.StartsWith("#") ||
                    trimmed.StartsWith("//") ||
                    trimmed.ToLower() == "requerimiento" ||
                    trimmed.ToLower() == "requerimientos")
                {
                    continue;
                }

                validRequirements.Add(trimmed);
            }

            if (validRequirements.Count == 0)
            {
                Debug.LogWarning("⚠️ No se encontraron requerimientos válidos en el archivo.");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"✅ Archivo leído: {validRequirements.Count} requerimientos encontrados");

            // ✅ Iniciar corrutina para instanciar con delay
            StartCoroutine(InstantiatePostItsWithDelay(validRequirements.ToArray()));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al leer archivo: {e.Message}");
        }
    }

    /// <summary>
    /// Crea los post-its en la red CON DELAY entre cada uno
    /// </summary>
    IEnumerator InstantiatePostItsWithDelay(string[] requirements)
    {
        if (isLoadingPostIts)
        {
            Debug.LogWarning("⚠️ Ya hay una carga en progreso. Cancelando...");
            yield break;
        }

        isLoadingPostIts = true;

        if (whiteboardParent == null)
        {
            Debug.LogError("❌ whiteboardParent no asignado. Asigna la pizarra en el Inspector.");
            isLoadingPostIts = false;
            yield break;
        }

        if (postItPrefab == null)
        {
            Debug.LogError("❌ postItPrefab no asignado.");
            isLoadingPostIts = false;
            yield break;
        }

        if (showDebugLogs)
            Debug.Log($"📝 Iniciando carga de {requirements.Length} requerimientos...");

        // ✅ Esperar un frame inicial para asegurar que la escena esté lista
        yield return new WaitForSeconds(0.5f);

        int successCount = 0;

        for (int i = 0; i < requirements.Length; i++)
        {
            // Calcular posición en grid
            int row = i / postItsPerRow;
            int column = i % postItsPerRow;

            // Posición LOCAL respecto a la pizarra
            Vector3 localPosition = new Vector3(
                startPosition.x + (column * spacingX),
                startPosition.y - (row * spacingY),
                postItZOffset
            );

            // Convertir a posición MUNDIAL
            Vector3 worldPosition = whiteboardParent.TransformPoint(localPosition);

            // Rotación alineada con la pizarra (girada 180° para que el texto mire hacia adelante)
            Quaternion worldRotation = whiteboardParent.rotation * Quaternion.Euler(0, 180, 0);

            // Datos a enviar al post-it
            object[] instantiationData = new object[] {
                requirements[i],  // [0] = Texto del requerimiento
                postItScale       // [1] = Escala personalizada
            };

            try
            {
                // Instanciar en red
                GameObject postIt = PhotonNetwork.Instantiate(
                    postItPrefab.name,
                    worldPosition,
                    worldRotation,
                    0, // Grupo por defecto
                    instantiationData
                );

                if (postIt != null)
                {
                    successCount++;
                    if (showDebugLogs)
                        Debug.Log($"✅ Post-it {i + 1}/{requirements.Length} creado: '{requirements[i]}'");
                }
                else
                {
                    Debug.LogError($"❌ Falló la creación del post-it {i + 1}: '{requirements[i]}'");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al instanciar post-it {i + 1}: {e.Message}");
            }

            // ✅ CRUCIAL: Esperar entre cada instanciación
            yield return new WaitForSeconds(delayBetweenPostIts);
        }

        if (showDebugLogs)
            Debug.Log($"🎉 Carga completada: {successCount}/{requirements.Length} post-its creados exitosamente.");

        isLoadingPostIts = false;
    }

    /// <summary>
    /// Función auxiliar para cargar requerimientos manualmente (para testing)
    /// </summary>
    [ContextMenu("Cargar Requerimientos de Prueba")]
    void LoadTestRequirements()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Solo el Master Client puede cargar requerimientos.");
            return;
        }

        string[] testRequirements = new string[] {
            "Login de usuarios",
            "Sistema de notificaciones",
            "Panel de administración",
            "Base de datos MySQL",
            "API REST",
            "Autenticación JWT",
            "Dashboard analítico",
            "Exportación de reportes",
            "Logs de auditoría",
            "Sistema de permisos"
        };

        StartCoroutine(InstantiatePostItsWithDelay(testRequirements));
    }

    /// <summary>
    /// Limpiar todos los post-its de la escena (útil para testing)
    /// </summary>
    [ContextMenu("Limpiar Todos los Post-its")]
    void ClearAllPostIts()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Solo el Master Client puede limpiar post-its.");
            return;
        }

        StickyNote[] allPostIts = FindObjectsOfType<StickyNote>();
        foreach (StickyNote postIt in allPostIts)
        {
            if (postIt.GetComponent<PhotonView>() != null)
            {
                PhotonNetwork.Destroy(postIt.gameObject);
            }
        }

        Debug.Log($"🗑️ {allPostIts.Length} post-its eliminados");
    }
}