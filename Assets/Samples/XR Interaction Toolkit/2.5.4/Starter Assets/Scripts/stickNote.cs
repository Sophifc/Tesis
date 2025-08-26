using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


[RequireComponent(typeof(Rigidbody))]
public class StickyNote : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isStuck = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isStuck = false;
        rb.isKinematic = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!isStuck && collision.gameObject.CompareTag("Pizarra"))
        {
            isStuck=true;

            //CONGELA EL RIGIBODY
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            //POSICIONA EL POST TIS EN LA PIZARRA
            ContactPoint contact = collision.contacts[0];
            transform.position = contact.point + contact.normal * 0.01f;
            transform.rotation = Quaternion.LookRotation(-contact.normal);

            transform.SetParent(collision.transform);
        }
        
    }
}