using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    // En el Inspector, arrastraremos nuestro prefab de "XR Origin" aquí.
    // Pero para Photon, es mejor usar el nombre del prefab en la carpeta Resources.

    public string playerPrefabName = "XR Origin (XR Rig) (1)"; // ¡El nombre debe ser exacto!

    void Start()
    {
        // Le decimos a Photon que cree una instancia de nuestro prefab para nosotros.
        // Photon se encargará de crearlo en el mismo punto para todos los jugadores.
        Debug.Log("Creando jugador desde Resources...");
        PhotonNetwork.Instantiate(playerPrefabName, Vector3.zero, Quaternion.identity);
    }
}
