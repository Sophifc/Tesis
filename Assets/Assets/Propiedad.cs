using UnityEngine;
using Photon.Pun; // Necesario para MonoBehaviourPun y photonView
using UnityEngine.XR.Interaction.Toolkit; // Para deshabilitar los mandos
using UnityEngine.InputSystem.XR; // Para el TrackedPoseDriver (seguimiento de cabeza)

// Heredamos de MonoBehaviourPun para tener acceso a "photonView"
public class AvatarOwnership : MonoBehaviourPun
{
    [Header("Visuales Remotos (Solo los demás los ven)")]
    //public GameObject cabezaRemota; // Arrastra la esfera "HeadVisuals" aquí
    //public GameObject manoIzquierdaRemota; // Arrastra la esfera de la mano izq.
    //public GameObject manoDerechaRemota; // Arrastra el modelo de la mano derecha
    public GameObject avatarRemoto; // Aquí irá tu Avatar KayKit completo

    [Header("Visuales Locales (Solo yo los veo)")]
    public GameObject cuerpoLocal;

    void Start()
    {
        // "photonView.IsMine" es la magia. 
        // Es TRUE si este avatar es del jugador local.
        // Es FALSE si es de un jugador remoto.

        if (photonView.IsMine)
        {
            // --- SOY YO (EL JUGADOR LOCAL) ---

            // 1. Muestro mi cuerpo local
            if (cuerpoLocal != null) cuerpoLocal.SetActive(true);

            // 2. Oculto mis visuales remotos (no quiero ver mi propia cabeza-esfera)
            if (avatarRemoto != null) avatarRemoto.SetActive(false); // Oculto el avatar completo
        }
        else
        {
            // --- ES OTRO JUGADOR (EL REMOTO) ---

            // 1. Oculto su cuerpo local (no necesito ver su "cuerpo falso")
            if (cuerpoLocal != null) cuerpoLocal.SetActive(false);

            // 2. Muestro sus visuales remotos (¡aquí está la clave!)
            //    Asegúrate de que las esferas estén ACTIVADAS por defecto en el prefab.
            if (avatarRemoto != null) avatarRemoto.SetActive(true); // Muestro el avatar completo

            // 3. Desactivo todos sus componentes de control (como ya hacíamos)
            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;

            TrackedPoseDriver headTracker = GetComponentInChildren<TrackedPoseDriver>();
            if (headTracker != null) headTracker.enabled = false;

            UnityEngine.XR.Interaction.Toolkit.XRController[] controllers = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRController>();
            foreach (UnityEngine.XR.Interaction.Toolkit.XRController controller in controllers)
            {
                controller.enabled = false;
            }

            XRRayInteractor[] interactors = GetComponentsInChildren<XRRayInteractor>();
            foreach (XRRayInteractor interactor in interactors)
            {
                interactor.enabled = false;
            }
        }
    }
}