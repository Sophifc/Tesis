using UnityEngine;
using Photon.Pun; // Necesario para MonoBehaviourPun y photonView
using UnityEngine.XR.Interaction.Toolkit; // Para deshabilitar los mandos
using UnityEngine.InputSystem.XR; // Para el TrackedPoseDriver (seguimiento de cabeza)

// Heredamos de MonoBehaviourPun para tener acceso a "photonView"
public class AvatarOwnership : MonoBehaviourPun
{
    [Header("Objetos Visuales (Cuerpo)")]
    public GameObject headVisuals; // Arrastra aquí la esfera de la cabeza
    public GameObject leftHandVisuals; // Arrastra el modelo de la mano izquierda
    public GameObject rightHandVisuals; // Arrastra el modelo de la mano derecha

    void Start()
    {
        // "photonView.IsMine" es la magia. 
        // Es TRUE si este avatar es del jugador local.
        // Es FALSE si es de un jugador remoto.

        if (photonView.IsMine)
        {
            // Este es MI avatar.
            // No quiero ver mi propia cabeza o manos flotando delante de mis ojos.
            if (headVisuals != null) headVisuals.SetActive(false);
            if (leftHandVisuals != null) leftHandVisuals.SetActive(false);
            if (rightHandVisuals != null) rightHandVisuals.SetActive(false);
        }
        else
        {
            // Este es el avatar de OTRA PERSONA.
            // Debemos deshabilitar todos los componentes que leen
            // el hardware local (cámara, mandos, etc.), porque este
            // avatar solo debe recibir datos de la red.

            // Desactiva la cámara y el "oído"
            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;

            // Desactiva el seguimiento de la cabeza (TrackedPoseDriver)
            TrackedPoseDriver headTracker = GetComponentInChildren<TrackedPoseDriver>();
            if (headTracker != null) headTracker.enabled = false;

            // Desactiva los mandos de interacción
            UnityEngine.XR.Interaction.Toolkit.XRController[] controllers = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRController>();
            foreach (UnityEngine.XR.Interaction.Toolkit.XRController controller in controllers)
            {
                controller.enabled = false;
            }
            // ======================================================================

            // Desactiva los rayos interactores
            XRRayInteractor[] interactors = GetComponentsInChildren<XRRayInteractor>();
            foreach (XRRayInteractor interactor in interactors)
            {
                interactor.enabled = false;
            }
        }
    }
}