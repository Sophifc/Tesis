using UnityEngine;
using Photon.Pun; // Necesario para MonoBehaviourPun y photonView

public class SeguimientoAvatar : MonoBehaviourPun // Hereda de MonoBehaviourPun
{

    [Header("Componentes del Avatar")]
    public Animator avatarAnimator; // Arrastra el Animator de AvatarRemoto aquí
    public CharacterController characterController;

    [Header("Referencias de VR (Tu Hardware)")]
    public Transform vrHead; // Tu Main Camera
    public Transform vrHandLeft; // Tu Left Controller
    public Transform vrHandRight; // Tu Right Controller

    [Header("Huesos del Esqueleto del Avatar")]
    public Transform avatarHead; // El hueso "Head" del Rig_Medium
    public Transform avatarHandLeft; // El hueso "LeftHand" del Rig_Medium
    public Transform avatarHandRight; // El hueso "RightHand" del Rig_Medium
    public Transform avatarHips; // <-- 1. AÑADE ESTA NUEVA LÍNEA

    void Update()
    {
        // Solo ejecutamos esto si somos el "dueño" de este avatar
        if (photonView.IsMine)
        {
            // Hacemos que los huesos del esqueleto sigan al hardware de VR
            // Esto "anima" el avatar localmente.
            // Photon Transform View se encargará de enviar este movimiento por la red.

            if (vrHead != null)
            {
                avatarHead.position = vrHead.position;
                avatarHead.rotation = vrHead.rotation;
            }

            if (vrHandLeft != null)
            {
                avatarHandLeft.position = vrHandLeft.position;
                avatarHandLeft.rotation = vrHandLeft.rotation;
            }

            if (vrHandRight != null)
            {
                avatarHandRight.position = vrHandRight.position;
                avatarHandRight.rotation = vrHandRight.rotation;
            }
            if (avatarHips != null && vrHead != null)
            {
                // Mueve las caderas para que coincidan horizontalmente con la cabeza
                Vector3 newHipsPos = avatarHips.position;
                newHipsPos.x = vrHead.position.x;
                newHipsPos.z = vrHead.position.z;
                // Dejamos avatarHips.position.y para que el cuerpo no vuele
                // (puedes ajustarlo si el avatar está en el suelo)

                // Una forma más simple de mantenerlo en el suelo:
                // Asumimos que el "suelo" del jugador de VR está en Y=0 
                // relativo al XR Origin.
                newHipsPos = new Vector3(vrHead.position.x, avatarHips.position.y, vrHead.position.z);

                avatarHips.position = newHipsPos;


                // Rota las caderas para que miren en la misma dirección que la cabeza
                // (pero solo horizontalmente, no queremos que el cuerpo se incline)
                Vector3 headForward = vrHead.forward;
                headForward.y = 0; // Ignora la inclinación vertical

                avatarHips.rotation = Quaternion.LookRotation(headForward);
            }

            if (avatarAnimator != null && characterController != null)
            {
                // 1. Mide la velocidad horizontal (ignora saltar/caer)
                Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);

                // 2. Calcula la velocidad (magnitud)
                float speed = horizontalVelocity.magnitude;

                // 3. Pasa la velocidad al Animator
                avatarAnimator.SetFloat("Speed", speed);
            }
        }
    }
}
