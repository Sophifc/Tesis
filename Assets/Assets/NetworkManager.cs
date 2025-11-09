using UnityEngine;
//using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections; // <-- AÑADE ESTA LÍNEA para Corutinas
using UnityEngine.XR.Management; // <-- AÑADE ESTA LÍNEA para controlar XR

// Heredamos de MonoBehaviourPunCallbacks para recibir eventos de Photon automáticamente
public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI Login")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;


    public static string FilePathToLoad = "";

    [Header("UI Carga de Archivo")]
    public TMP_InputField filePathInput;
    // Para crear o unirse a una sala



    void Start()
    {
        // Conecta al servidor maestro de Photon al iniciar el juego
        Debug.Log("Conectando a Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // Esta función se llama automáticamente cuando nos conectamos al servidor maestro
    public override void OnConnectedToMaster()
    {
        Debug.Log("¡Conectado al Servidor Maestro de Photon!");
        // Habilitamos los botones de la UI una vez conectados
        PhotonNetwork.AutomaticallySyncScene = true; // Sincroniza la escena para todos
    }

    // --- FUNCIONES PARA LOS BOTONES DE LA UI ---

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("El nombre de usuario y el código de la sala no pueden estar vacíos.");
            return;
        }

        if (!string.IsNullOrEmpty(filePathInput.text))
        {
            FilePathToLoad = filePathInput.text.Trim('"');
        }

        PhotonNetwork.NickName = playerNameInput.text; // Guardamos el nombre del jugador
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10; // Límite de 10 jugadores por sala
        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        Debug.Log("Creando sala: " + roomNameInput.text);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("El nombre de usuario y el código de la sala no pueden estar vacíos.");
            return;
        }

        PhotonNetwork.NickName = playerNameInput.text;
        PhotonNetwork.JoinRoom(roomNameInput.text);
        Debug.Log("Uniéndose a la sala: " + roomNameInput.text);
    }

    /// <summary>
    /// Cierra la aplicación del juego por completo.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Cerrando la aplicación...");
        Application.Quit();
    }

    // --- CALLBACKS DE PHOTON ---

    // Se llama cuando nos unimos exitosamente a una sala
    public override void OnJoinedRoom()
    {
        Debug.Log("¡Te has unido a la sala: " + PhotonNetwork.CurrentRoom.Name + "!");
        Debug.Log("Tu nombre es: " + PhotonNetwork.NickName);

        // Cargamos la escena principal para todos los jugadores
        // Solo el "MasterClient" (el creador de la sala) puede cargar la escena
        //if (PhotonNetwork.IsMasterClient)
        //{
        //    PhotonNetwork.LoadLevel("SampleScene"); // Reemplaza "SampleScene" con el nombre de tu escena principal
        //}
        StartCoroutine(StartVRAndLoadScene());

    }

    // ======================================================================
    // ESTA ES LA NUEVA FUNCIÓN PARA ACTIVAR VR
    // ======================================================================
    IEnumerator StartVRAndLoadScene()
    {
        Debug.Log("Iniciando el sistema XR (VR)...");

        // 1. Inicializa el cargador de XR
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        // 2. Inicia los subsistemas de XR (activa la VR)
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            yield return null; // Espera un frame para que se estabilice
            Debug.Log("¡VR Iniciada! El usuario debe ponerse el casco.");
        }
        else
        {
            Debug.LogWarning("No se pudo iniciar el cargador de XR. Cargando escena sin VR.");
        }

        // 3. Ahora que VR está activa, cargamos la escena principal
        // Solo el MasterClient puede cargar la escena
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene"); // Reemplaza "SampleScene" con el nombre de tu escena principal
        }
    }


    // Se llama si falla la unión a una sala
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Fallo al unirse a la sala: " + message);
    }
}
