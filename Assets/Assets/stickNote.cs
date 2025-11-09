using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun; // <-- Asegúrate de que esté
using TMPro; // <-- Asegúrate de que esté


[RequireComponent(typeof(Rigidbody))]
public class StickyNote : MonoBehaviour, IPunInstantiateMagicCallback
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isStuck = false;
    private Vector3 correctScale;

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
        transform.SetParent(null);
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
            if (correctScale != Vector3.zero) // Asegura que la escala haya sido guardada
            {
                transform.localScale = correctScale;
            }
        }
        
    }

    // Pegar y despegar reuqerimientos cargados en post-its
    public void Stick()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log(gameObject.name + " está ahora pegado (Kinematic).");
        }
    }

    public void Unstick()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            Debug.Log(gameObject.name + " ha sido despegado.");
        }
    }

    // ======================================================================
    // ESTA ES LA NUEVA FUNCIÓN MÁGICA DE PHOTON
    // ======================================================================
    /// <summary>
    /// Esta función es llamada automáticamente por Photon en TODOS los clientes
    /// (incluido el Master Client) justo cuando se crea este objeto.
    /// </summary>
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 1. Recibimos los datos de instanciación (el texto y la escala)
        object[] data = info.photonView.InstantiationData;
        if (data != null)
        {
            if (data.Length > 0)
            {
                // Asignamos el texto
                string requirementText = (string)data[0];
                TMP_InputField postItInputField = GetComponentInChildren<TMP_InputField>();
                if (postItInputField != null)
                {
                    postItInputField.text = requirementText;
                }
            }
            if (data.Length > 1)
            {
                // Asignamos la escala
                //transform.localScale = (Vector3)data[1];
                correctScale = (Vector3)data[1];
            }
        }

        // 2. Buscamos la pizarra en la escena
        // (Asegúrate de que tu pizarra tenga el Tag "Pizarra")
        Transform whiteboardParent = GameObject.FindGameObjectWithTag("Pizarra").transform;

        // 3. Lo emparentamos y lo "pegamos"
        if (whiteboardParent != null)
        {
            transform.SetParent(whiteboardParent);
            if (correctScale != Vector3.zero)
            {
                transform.localScale = correctScale;
            }
            // Como ya fue creado en la posición y rotación correctas,
            // solo necesitamos hacerlo cinemático para que se "pegue".
            isStuck = true;
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError("StickyNote no pudo encontrar un objeto con el Tag 'Pizarra' al instanciarse.");
        }
    }
}