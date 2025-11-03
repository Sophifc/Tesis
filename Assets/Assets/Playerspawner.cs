using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public string playerPrefabName = "XR Origin (XR Rig) (1)";

    // ¡NUEVA VARIABLE!
    [Header("Punto de Spawn")]
    [Tooltip("Arrastra aquí el objeto vacío que marca dónde debe aparecer el jugador")]
    public Transform spawnPoint; // El "Transform" guarda tanto la posición como la rotación

    void Start()
    {
        // Una comprobación para evitar errores
        if (spawnPoint == null)
        {
            Debug.LogError("¡No se ha asignado un SpawnPoint al PlayerSpawner! El jugador aparecerá en (0,0,0).");
            // Usamos la posición del propio Spawner como plan B
            spawnPoint = this.transform;
        }

        Debug.Log("Creando jugador en el punto de spawn...");

        // ¡LÍNEA MODIFICADA!
        // Ahora usamos la posición Y la rotación de nuestro objeto SpawnPoint
        PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
    }
}