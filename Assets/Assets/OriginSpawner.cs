using UnityEngine;
using Photon.Pun;

/// <summary>
/// Instancia el XR Origin del jugador local cuando entra a la sala
/// Y luego instancia el avatar visual
/// </summary>
public class XROriginSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefabs")]
    [Tooltip("Nombre del prefab XR Origin en Resources/")]
    public string xrOriginPrefabName = "XR Origin (XR Rig) (1)";

    [Tooltip("Nombre del prefab Avatar en Resources/")]
    public string avatarPrefabName = "Avatar";

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private GameObject localXROrigin;
    private GameObject localAvatar;
    private bool hasSpawned = false;

    void Start()
    {
        // Solo spawning cuando estemos conectados y en una sala
        if (PhotonNetwork.InRoom && !hasSpawned)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        // Spawning cuando entramos a la sala
        if (!hasSpawned)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        if (hasSpawned)
        {
            Debug.LogWarning("⚠️ Jugador ya fue spawneado");
            return;
        }

        // Determinar posición de spawn
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Transform spawnPoint = spawnPoints[spawnIndex];
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }

        // ✅ PASO 1: Crear XR Origin LOCAL (solo para este jugador)
        GameObject xrOriginPrefab = Resources.Load<GameObject>(xrOriginPrefabName);

        if (xrOriginPrefab == null)
        {
            Debug.LogError($"❌ No se encontró el prefab '{xrOriginPrefabName}' en Resources/");
            return;
        }

        localXROrigin = Instantiate(xrOriginPrefab, spawnPosition, spawnRotation);
        localXROrigin.name = $"XR Origin (Local - {PhotonNetwork.NickName})";

        Debug.Log($"✅ XR Origin local creado para {PhotonNetwork.NickName}");

        // Verificar cámara
        Camera cam = localXROrigin.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.tag = "MainCamera";
            cam.enabled = true;
            Debug.Log($"✅ Cámara configurada: {cam.name}");
        }
        else
        {
            Debug.LogError("❌ No se encontró cámara en el XR Origin!");
        }

        // ✅ PASO 2: Esperar un frame y luego crear avatar visual EN RED
        StartCoroutine(SpawnAvatarNextFrame());
    }

    System.Collections.IEnumerator SpawnAvatarNextFrame()
    {
        yield return null; // Esperar un frame para que XR Origin se inicialice

        // Instanciar avatar visual EN RED (todos lo verán)
        GameObject avatarPrefab = Resources.Load<GameObject>(avatarPrefabName);

        if (avatarPrefab != null)
        {
            localAvatar = PhotonNetwork.Instantiate(
                avatarPrefabName,
                Vector3.zero,
                Quaternion.identity
            );

            Debug.Log($"✅ Avatar visual de {PhotonNetwork.NickName} creado");
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró el prefab '{avatarPrefabName}' en Resources/");
        }

        hasSpawned = true;
    }

    void OnDestroy()
    {
        // Limpiar al salir
        if (localXROrigin != null)
        {
            Destroy(localXROrigin);
        }
    }
}
