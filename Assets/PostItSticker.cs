using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PostItSticker : XRGrabInteractable
{
    public float stickDistance = 0.1f; // Distancia máxima para pegar
    private bool isStuck = false;

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (isStuck) return;

        // Verifica si está cerca de la pizarra
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, stickDistance))
        {
            if (hit.collider.CompareTag("Pizarra"))
            {
                // Pega el Post-It
                transform.position = hit.point;
                transform.rotation = Quaternion.LookRotation(-hit.normal);
                isStuck = true;
                GetComponent<Rigidbody>().isKinematic = true; // Congela la física
            }
        }
    }
}