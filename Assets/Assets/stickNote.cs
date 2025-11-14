using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;
using TMPro;
using UnityEngine.InputSystem;



[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class StickyNote : MonoBehaviour, IPunInstantiateMagicCallback
{
    // --- Referencias de Componentes ---
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private PhotonView photonView;
    private Vector3 correctScale;

    [Header("Configuración de Edición")]
    public Color[] availableColors = new Color[] {
        new Color(1, 1, 0.8f),
        new Color(1, 0.8f, 1),
        new Color(0.8f, 1, 1)
    };
    public float scaleSpeed = 0.5f;

    [Header("Acciones de Input (solo VR real)")]
    public InputActionProperty leftThumbstick;   // Joystick Izquierdo
    public InputActionProperty rightThumbstick;  // Joystick Derecho

    [Header("Modo simulador (teclado para pruebas sin visor)")]
    public bool simulatorMode = true;  // ✅ Activa esto para probar con teclado

    // --- Variables Privadas de Estado ---
    private XRBaseInteractor holdingInteractor = null;
    private bool isTouchingPizarra = false;
    private Transform lastPizarraTouched = null;
    private int currentColorIndex = 0;
    private bool joystickUsedX = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        // ✅ Inicializar escala base
        correctScale = transform.localScale;
    }

    private void Update()
    {
        // Solo el "dueño" del Post-it puede modificarlo
        if (holdingInteractor != null && photonView.IsMine)
        {
            // ==========================================================
            // --- MODO SIMULADOR ---
            // ==========================================================
            if (Keyboard.current != null)
            {
                if (Keyboard.current.equalsKey != null && Keyboard.current.equalsKey.isPressed)
                    ApplyScaleChange(scaleSpeed * Time.deltaTime);

                if (Keyboard.current.minusKey != null && Keyboard.current.minusKey.isPressed)
                    ApplyScaleChange(-scaleSpeed * Time.deltaTime);

                if (Keyboard.current.leftArrowKey != null && Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    ChangeColor(-1);

                if (Keyboard.current.rightArrowKey != null && Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    ChangeColor(1);
            }

            // ==========================================================
            // --- MODO VR REAL (usa Input Actions del XRI) ---
            // ==========================================================
            InputAction thumbstickAction = null; // Joystick para editar (de la OTRA mano)

            if (holdingInteractor.gameObject.name.Contains("Left"))
            {
                thumbstickAction = rightThumbstick.action; // Si agarro con izquierda, uso joystick derecho
            }
            else if (holdingInteractor.gameObject.name.Contains("Right"))
            {
                thumbstickAction = leftThumbstick.action; // Si agarro con derecha, uso joystick izquierdo
            }

            // Si no hay joystick asignado, salir
            if (thumbstickAction == null) return;

            // Leemos entrada del joystick
            Vector2 input = thumbstickAction.ReadValue<Vector2>();

            // --- ESCALAR (Eje vertical del joystick) ---
            if (Mathf.Abs(input.y) > 0.3f) // Umbral ajustado
            {
                ApplyScaleChange(input.y * scaleSpeed * Time.deltaTime);
            }

            // --- CAMBIAR COLOR (Eje horizontal) ---
            if (Mathf.Abs(input.x) > 0.8f && !joystickUsedX)
            {
                joystickUsedX = true;
                if (input.x > 0) ChangeColor(1);
                else ChangeColor(-1);
            }
            else if (Mathf.Abs(input.x) < 0.2f)
            {
                joystickUsedX = false;
            }
        }
    }

    // --------------------------------------------------------------
    // FUNCIONES AUXILIARES
    // --------------------------------------------------------------
    void ApplyScaleChange(float scaleAmount)
    {
        Vector3 newScale = transform.localScale + (Vector3.one * scaleAmount);
        newScale.x = Mathf.Clamp(newScale.x, 0.1f, 2.0f);
        newScale.y = newScale.x;
        newScale.z = 0.02f; // mantener grosor fijo
        transform.localScale = newScale;
        correctScale = newScale;

        // Sincronizar escala en red
        photonView.RPC("SyncScale", RpcTarget.OthersBuffered, newScale);
    }

    void ChangeColor(int direction)
    {
        currentColorIndex = (currentColorIndex + direction + availableColors.Length) % availableColors.Length;
        photonView.RPC("SyncColor", RpcTarget.AllBuffered, currentColorIndex);
    }

    // --------------------------------------------------------------
    // SINCRONIZACIÓN PHOTON
    // --------------------------------------------------------------
    [PunRPC]
    void SyncScale(Vector3 newScale)
    {
        transform.localScale = newScale;
        correctScale = newScale;
    }

    [PunRPC]
    void SyncColor(int colorIndex)
    {
        currentColorIndex = colorIndex;
        Color newColor = availableColors[currentColorIndex];
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
        }
    }

    // --------------------------------------------------------------
    // INTERACCIÓN XR
    // --------------------------------------------------------------
    void OnGrab(SelectEnterEventArgs args)
    {
        rb.isKinematic = false;
        transform.SetParent(null);
        holdingInteractor = args.interactorObject as XRBaseInteractor;

        // ✅ Transferir ownership al jugador que agarra
        if (!photonView.IsMine)
            photonView.RequestOwnership();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        holdingInteractor = null;
        if (isTouchingPizarra && lastPizarraTouched != null)
        {
            Stick(lastPizarraTouched);
        }
        else
        {
            rb.isKinematic = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pizarra"))
        {
            isTouchingPizarra = true;
            lastPizarraTouched = collision.transform;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pizarra"))
        {
            isTouchingPizarra = false;
            lastPizarraTouched = null;
        }
    }

    public void Stick(Transform parentSurface)
    {
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetParent(parentSurface);
        if (correctScale != Vector3.zero)
        {
            transform.localScale = correctScale;
        }
    }

    // --------------------------------------------------------------
    // INICIALIZACIÓN PHOTON
    // --------------------------------------------------------------
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data != null)
        {
            if (data.Length > 0)
            {
                string requirementText = (string)data[0];
                TMP_InputField postItInputField = GetComponentInChildren<TMP_InputField>();
                if (postItInputField != null)
                    postItInputField.text = requirementText;
            }
            if (data.Length > 1)
            {
                correctScale = (Vector3)data[1];
            }
        }

        // Color inicial y sincronización global
        GetComponentInChildren<MeshRenderer>().material.color = availableColors[currentColorIndex];
        photonView.RPC("SyncColor", RpcTarget.OthersBuffered, currentColorIndex);

        Transform whiteboardParent = GameObject.FindGameObjectWithTag("Pizarra").transform;
        if (whiteboardParent != null)
        {
            Stick(whiteboardParent);
        }
    }
}
