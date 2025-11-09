using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Management;

[RequireComponent(typeof(Canvas))] // Nos aseguramos de que el script esté en un Canvas
public class BackMenu : MonoBehaviourPunCallbacks
{
    [Tooltip("El nombre de tu escena 2D del menú principal")]
    public string menuSceneName = "Menu";

    private Canvas worldCanvas; // Variable para nuestro Canvas

    // AÑADE ESTA NUEVA FUNCIÓN
    void Start()
    {
        // Obtenemos el Canvas en el que está este script
        worldCanvas = GetComponent<Canvas>();

        // Iniciamos la corutina para encontrar la cámara de VR
        // cuando haya sido creada por el PlayerSpawner.
        StartCoroutine(AssignEventCamera());
    }

    // AÑADE ESTA NUEVA CORUTINA
    private IEnumerator AssignEventCamera()
    {
        // Esperamos un par de segundos para asegurarnos de que el
        // PlayerSpawner haya creado nuestro avatar y el script "Propiedad.cs"
        // haya desactivado las cámaras de los otros jugadores.
        yield return new WaitForSeconds(2.0f);

        try
        {
            // Camera.main busca la primera CÁMARA ACTIVA que tenga el Tag "MainCamera".
            // Gracias a tu script "Propiedad.cs", la única cámara activa 
            // con ese tag será la del jugador local (la tuya).
            Camera vrCamera = Camera.main;

            if (vrCamera != null)
            {
                // Asignamos la cámara encontrada al campo "Event Camera" del Canvas
                worldCanvas.worldCamera = vrCamera;
                Debug.Log("Event Camera asignada exitosamente al Canvas: " + vrCamera.name);
            }
            else
            {
                Debug.LogWarning("No se pudo encontrar una 'MainCamera' activa para asignar al Canvas. Los botones de UI podrían no funcionar.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al asignar la Event Camera: " + e.Message);
        }
    }

    // Esta es la función pública que llamará tu botón
    public void OnClick_LeaveSession()
    {
        Debug.Log("Saliendo de la sala de Photon...");
        // Esto le dice a Photon que abandone la sala actual
        PhotonNetwork.LeaveRoom();
    }

    // Photon llama a esta función automáticamente cuando hemos salido
    public override void OnLeftRoom()
    {
        Debug.Log("Se ha salido de la sala. Volviendo al menú.");
        // Iniciamos la corutina para apagar la VR y cargar el menú
        StartCoroutine(StopVRAndLoadMenu());
    }

    private IEnumerator StopVRAndLoadMenu()
    {
        // Apaga los subsistemas de XR (desactiva la VR)
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("Deteniendo subsistemas XR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            yield return null; // Espera un frame
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("Subsistemas XR detenidos.");
        }

        // Carga la escena del menú 2D
        SceneManager.LoadScene(menuSceneName);
    }
}
