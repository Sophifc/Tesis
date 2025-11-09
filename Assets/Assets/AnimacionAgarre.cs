using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // ¡Necesario para XR!
using Photon.Pun; // ¡Necesario para Photon!

// Asegúrate de que el nombre de la clase sea "AnimacionAgarre"
public class AnimacionAgarre : MonoBehaviour
{
    private Animator avatarAnimator;
    private PhotonView photonView;

    // Esta variable guardará TODOS los interactores de la mano
    // (el rayo, el de toque, el de teletransporte, etc.)
    private XRBaseInteractor[] interactors;

    void Start()
    {
        // 1. Busca el PhotonView y el Animator en el objeto RAÍZ (el XR Origin)
        photonView = GetComponentInParent<PhotonView>();

        if (photonView != null && photonView.IsMine)
        {
            // Busca el "cerebro" (Animator) en el objeto raíz del avatar
            avatarAnimator = photonView.GetComponent<Animator>();
        }
        else
        {
            // Si no es mi avatar, este script no debe hacer nada.
            this.enabled = false;
            return;
        }

        // 2. Busca TODOS los interactores (Ray, Direct, Poke, etc.)
        //    que están en este objeto o en sus hijos.
        interactors = GetComponentsInChildren<XRBaseInteractor>();

        if (avatarAnimator == null)
        {
            Debug.LogWarning("AnimacionAgarre: No se encontró el Animator en la raíz.");
            return;
        }

        // 3. ¡LA CORRECCIÓN!
        //    Nos "suscribimos" al evento de CADA interactor.
        foreach (XRBaseInteractor interactor in interactors)
        {
            // Le decimos que llame a "OnGrab" cuando agarre algo
            interactor.selectEntered.AddListener(OnGrab);

            // Le decimos que llame a "OnRelease" cuando suelte algo
            interactor.selectExited.AddListener(OnRelease);
        }
    }

    // Se llama cuando CUALQUIER interactor de esta mano agarra algo
    private void OnGrab(SelectEnterEventArgs args)
    {
        if (avatarAnimator != null)
        {
            // Disparamos el trigger "Agarrar" que creamos en el Animator
            avatarAnimator.SetTrigger("Agarrar");
        }
    }

    // Se llama cuando CUALQUIER interactor de esta mano suelta algo
    private void OnRelease(SelectExitEventArgs args)
    {
        if (avatarAnimator != null)
        {
            // (Opcional) Si tienes un trigger de "Soltar" en tu Animator,
            // puedes dispararlo aquí.
            // avatarAnimator.SetTrigger("Soltar"); 
        }
    }
}