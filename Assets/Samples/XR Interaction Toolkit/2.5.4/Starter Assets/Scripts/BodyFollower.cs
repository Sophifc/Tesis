using UnityEngine;

public class BodyFollower : MonoBehaviour
{
    public Transform cameraTransform;
    public float smooth = 5f;

    void Update()
    {
        if (cameraTransform == null) return;

        // Posición: sigue a la cámara en XZ, pero fija en Y=0 (suelo)
        Vector3 targetPos = new Vector3(cameraTransform.position.x, 0, cameraTransform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smooth);

        // Rotación: mira hacia donde mira la cámara (en horizontal)
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.01f)
            transform.forward = forward;
    }
}


