using UnityEngine;

public class VRPlayerCollider : MonoBehaviour
{
    public Transform xrCamera;       
    private CapsuleCollider capsule;
    private Rigidbody rb;

    void Start()
    {
        capsule = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Ajustar la altura del capsule al nivel de la cabeza
        capsule.height = xrCamera.localPosition.y;
        capsule.center = new Vector3(xrCamera.localPosition.x, capsule.height / 2f, xrCamera.localPosition.z);

        // Mantener el rigidbody en la misma base que la cámara
        Vector3 newPos = new Vector3(xrCamera.position.x, transform.position.y, xrCamera.position.z);
        rb.MovePosition(newPos);
    }
}
