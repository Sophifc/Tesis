using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkGrabbable : MonoBehaviour
{
    private PhotonView photonView;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Si no soy el dueño, pido la propiedad al agarrarlo
        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
        }
    }
}
