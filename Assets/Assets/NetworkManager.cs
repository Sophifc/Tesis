using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración de Archivo")]
    public static string FilePathToLoad;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private XROriginManager xrOriginManager;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        // Buscar el XR Origin Manager en la escena
        xrOriginManager = FindObjectOfType<XROriginManager>();

        if (xrOriginManager == null && showDebugLogs)
        {
            Debug.LogWarning("⚠️ XROriginManager no encontrado. Asegúrate de agregarlo al XR Origin.");
        }

        // Conectar a Photon si no está conectado
        if (!PhotonNetwork.IsConnected)
        {
            if (showDebugLogs)
                Debug.Log("📡 Conectando a Photon...");

            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("✅ Ya conectado a Photon");

            OnConnectedToMaster();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (showDebugLogs)
            Debug.Log("✅ Conectado a Photon Master Server");

        // Unirse o crear sala
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10;
        PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        if (showDebugLogs)
        {
            Debug.Log($"✅ Unido a sala: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"   Jugadores en sala: {PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        // Notificar al XR Origin Manager que puede crear el avatar
        if (xrOriginManager != null)
        {
            xrOriginManager.OnPhotonConnected();
        }
        else
        {
            Debug.LogError("❌ XROriginManager no encontrado. No se creará avatar visual.");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (showDebugLogs)
            Debug.Log($"👤 Jugador entró: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (showDebugLogs)
            Debug.Log($"👋 Jugador salió: {otherPlayer.NickName}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"⚠️ Desconectado de Photon: {cause}");
    }
}