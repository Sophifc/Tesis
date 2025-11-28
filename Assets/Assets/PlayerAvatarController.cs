using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.XR;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using System;

/// <summary>
/// Controla el avatar del jugador en VR - sincroniza cabeza y manos
/// VERSIÓN MEJORADA: Con animaciones de agarre y gestos de mano
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerAvatarController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Referencias del Avatar (Visuales)")]
    public Transform head;           // Tu objeto Head (Mannequin)
    public Transform body;           // Tu objeto Hips/Body
    public Transform leftHand;       // Tu objeto Hand.L
    public Transform rightHand;      // Tu objeto Hand.R

    [Header("Visibilidad Local")]
    public bool showOwnBody = false;           // Si true, ves tu propio cuerpo
    public bool highlightOwnHands = true;      // Si true, tus manos se ven más brillantes
    public Color ownHandsColor = Color.white;  // Color de tus propias manos
    private Color originalLeftColor;
    private Color originalRightColor;

    [Header("Configuración")]
    public TextMeshPro nameText;
    public float nameHeightOffset = 0.3f;
    public Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };

    [Header("Animación de Manos")]
    public bool enableHandAnimations = true;
    public float handGrabSpeed = 10f;
    public Vector3 handGrabScale = new Vector3(0.8f, 0.8f, 0.8f); // Escala al cerrar la mano
    private Vector3 leftHandOriginalScale;
    private Vector3 rightHandOriginalScale;

    [Header("Detección de Agarre")]
    public float grabDetectionRadius = 0.15f; // Radio para detectar si la mano está cerca de un objeto

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showHandGizmos = false;

    // --- Referencias Lógicas (Se llenan solas) ---
    private Transform xrCamera;
    private Transform xrLeftHand;
    private Transform xrRightHand;
    private XRController leftController;
    private XRController rightController;

    private bool isLocalPlayer = false;

    // Variables de Red (Interpolación)
    private Vector3 netHeadPos, netLHandPos, netRHandPos;
    private Quaternion netHeadRot, netLHandRot, netRHandRot;

    // Estado de agarre
    private bool isLeftHandGrabbing = false;
    private bool isRightHandGrabbing = false;
    private Transform grabbedObjectLeft = null;
    private Transform grabbedObjectRight = null;
    private float leftGripValue = 0f;
    private float rightGripValue = 0f;

    // ====================================================================
    // INICIALIZACIÓN
    // ====================================================================
    void Start()
    {
        isLocalPlayer = photonView.IsMine;

        if (showDebugInfo)
        {
            Debug.Log($"================================");
            Debug.Log($"Avatar inicializado:");
            Debug.Log($"  Jugador: {photonView.Owner.NickName}");
            Debug.Log($"  Es local: {isLocalPlayer}");
            Debug.Log($"  PhotonView ID: {photonView.ViewID}");
            Debug.Log($"================================");
        }

        // Guardar escalas originales de las manos
        if (leftHand != null) leftHandOriginalScale = leftHand.localScale;
        if (rightHand != null) rightHandOriginalScale = rightHand.localScale;

        // ✅ NUEVO: Guardar colores originales de las manos
        if (leftHand != null)
        {
            MeshRenderer leftRenderer = leftHand.GetComponentInChildren<MeshRenderer>();
            if (leftRenderer != null)
                originalLeftColor = leftRenderer.material.color;
        }
        if (rightHand != null)
        {
            MeshRenderer rightRenderer = rightHand.GetComponentInChildren<MeshRenderer>();
            if (rightRenderer != null)
                originalRightColor = rightRenderer.material.color;
        }

        if (isLocalPlayer)
        {
            InitializeLocalPlayer();
        }
        else
        {
            InitializeRemotePlayer();
        }

        // Configurar nombre
        if (nameText != null)
        {
            nameText.text = photonView.Owner.NickName;
            nameText.color = Color.white;

            // ✅ Ocultar nombre propio (opcional)
            if (isLocalPlayer)
            {
                nameText.gameObject.SetActive(false);
            }
        }

        // Asignar color al azar o por ID
        AssignPlayerColor();

        if (showDebugInfo)
        {
            Debug.Log($"✅ Avatar {photonView.Owner.NickName} completamente inicializado");
        }
    }

    void InitializeLocalPlayer()
    {
        Debug.Log("👤 Inicializando avatar LOCAL: Buscando XR Origin...");

        // 1. Buscar el componente XR Origin en la escena
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("❌ GRAVE: No se encontró el 'XR Origin' en la escena. El avatar no se moverá.");
            return;
        }

        // 2. Asignar Cámara
        xrCamera = xrOrigin.Camera.transform;

        // 3. Asignar Manos buscando en los hijos del XR Origin
        foreach (var controller in xrOrigin.GetComponentsInChildren<ActionBasedController>())
        {
            if (controller.name.Contains("Left") || controller.gameObject.CompareTag("LeftController"))
            {
                xrLeftHand = controller.transform;
                leftController = controller.GetComponent<XRController>();
                Debug.Log("✅ Mano Izquierda conectada");
            }
            else if (controller.name.Contains("Right") || controller.gameObject.CompareTag("RightController"))
            {
                xrRightHand = controller.transform;
                rightController = controller.GetComponent<XRController>();
                Debug.Log("✅ Mano Derecha conectada");
            }
        }

        // ✅ 4. OCULTAR TODO EL CUERPO EXCEPTO LAS MANOS
        // Estrategia simple: Ocultar TODO y luego mostrar solo las manos

        // Paso 1: Ocultar TODOS los renderers del avatar
        MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in allRenderers)
        {
            r.enabled = false;
        }

        // Paso 2: Mostrar SOLO los renderers de las manos
        if (leftHand != null)
        {
            MeshRenderer[] leftRenderers = leftHand.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in leftRenderers)
            {
                r.enabled = true;
            }
        }

        if (rightHand != null)
        {
            MeshRenderer[] rightRenderers = rightHand.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in rightRenderers)
            {
                r.enabled = true;
            }
        }

        // ✅ NUEVO: Resaltar las manos propias si está activado
        if (highlightOwnHands)
        {
            SetHandColor(leftHand, ownHandsColor);
            SetHandColor(rightHand, ownHandsColor);
        }

        if (showDebugInfo)
        {
            Debug.Log($"✅ Avatar LOCAL configurado:");
            Debug.Log($"   - Todo el cuerpo OCULTO");
            Debug.Log($"   - Solo manos VISIBLES");
            Debug.Log($"   - Renderers de mano izquierda: {(leftHand != null ? leftHand.GetComponentsInChildren<MeshRenderer>().Length : 0)}");
            Debug.Log($"   - Renderers de mano derecha: {(rightHand != null ? rightHand.GetComponentsInChildren<MeshRenderer>().Length : 0)}");
        }

        // 5. Ocultar rayos láser locales para que no molesten (opcional)
        var rays = xrOrigin.GetComponentsInChildren<LineRenderer>(true);
        foreach (var ray in rays) ray.enabled = false;

        if (showDebugInfo)
        {
            Debug.Log($"✅ Avatar local inicializado - Solo manos visibles");
            Debug.Log($"   Cámara: {xrCamera != null}");
            Debug.Log($"   Mano izquierda: {xrLeftHand != null}");
            Debug.Log($"   Mano derecha: {xrRightHand != null}");
        }
    }

    /// <summary>
    /// Verifica si un renderer pertenece a una mano
    /// </summary>
    bool IsHandRenderer(Transform t)
    {
        // Verificar si este transform o sus padres son las manos
        Transform current = t;
        while (current != null)
        {
            if (current == leftHand || current == rightHand)
                return true;
            current = current.parent;
        }
        return false;
    }

    /// <summary>
    /// Cambia el color de una mano
    /// </summary>
    void SetHandColor(Transform hand, Color color)
    {
        if (hand == null) return;

        foreach (var r in hand.GetComponentsInChildren<MeshRenderer>())
        {
            r.material.color = color;
        }
    }

    void InitializeRemotePlayer()
    {
        // Asegurar que el avatar remoto sea completamente visible
        MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in allRenderers)
        {
            r.enabled = true;
        }

        if (showDebugInfo)
        {
            Debug.Log($"✅ Avatar REMOTO configurado:");
            Debug.Log($"   - Todo el cuerpo VISIBLE");
            Debug.Log($"   - Total de renderers: {allRenderers.Length}");
        }
    }

    // ====================================================================
    // UPDATE - MOVIMIENTO Y ANIMACIONES
    // ====================================================================
    void Update()
    {
        if (isLocalPlayer)
        {
            UpdateLocalPlayerTransforms();
            UpdateHandGripInput();
            UpdateHandAnimations();
        }
        else
        {
            UpdateRemotePlayerTransforms();
        }

        // El nombre siempre mira a la cámara (Billboard)
        if (nameText != null && Camera.main != null)
        {
            nameText.transform.LookAt(Camera.main.transform);
            nameText.transform.Rotate(0, 180, 0); // Corregir espejo
        }
    }

    void UpdateLocalPlayerTransforms()
    {
        // Si no encontramos la cámara al inicio, no hacemos nada
        if (xrCamera == null) return;

        // 1. POSICIÓN GLOBAL: El avatar sigue al XR Origin (pies)
        Vector3 feetPos = xrCamera.position;
        feetPos.y = 0; // Mantener en el suelo
        transform.position = feetPos;

        // 2. CUERPO: Gira según hacia donde mira la cámara (solo eje Y)
        Vector3 lookDir = xrCamera.forward;
        lookDir.y = 0;
        if (lookDir != Vector3.zero && body != null)
        {
            body.rotation = Quaternion.Lerp(body.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // 3. CABEZA: Copia exactamente la rotación de la cámara
        if (head != null)
        {
            head.rotation = xrCamera.rotation;
            // Posicionar nombre
            if (nameText != null) nameText.transform.position = head.position + Vector3.up * nameHeightOffset;
        }

        // 4. MANOS: Copian la posición real de los mandos
        if (leftHand != null && xrLeftHand != null)
        {
            leftHand.position = xrLeftHand.position;
            leftHand.rotation = xrLeftHand.rotation;
        }

        if (rightHand != null && xrRightHand != null)
        {
            rightHand.position = xrRightHand.position;
            rightHand.rotation = xrRightHand.rotation;
        }
    }

    void UpdateRemotePlayerTransforms()
    {
        // Suavizar movimiento de jugadores remotos (Interpolación)
        float smooth = Time.deltaTime * 10f;

        if (head != null)
        {
            head.position = Vector3.Lerp(head.position, netHeadPos, smooth);
            head.rotation = Quaternion.Lerp(head.rotation, netHeadRot, smooth);
            if (nameText != null) nameText.transform.position = head.position + Vector3.up * nameHeightOffset;
        }

        if (leftHand != null)
        {
            leftHand.position = Vector3.Lerp(leftHand.position, netLHandPos, smooth);
            leftHand.rotation = Quaternion.Lerp(leftHand.rotation, netLHandRot, smooth);
        }

        if (rightHand != null)
        {
            rightHand.position = Vector3.Lerp(rightHand.position, netRHandPos, smooth);
            rightHand.rotation = Quaternion.Lerp(rightHand.rotation, netRHandRot, smooth);
        }
    }

    // ====================================================================
    // ANIMACIONES DE MANOS
    // ====================================================================
    void UpdateHandGripInput()
    {
        if (!enableHandAnimations) return;

        // Leer valores de grip de los controladores VR
        if (leftController != null)
        {
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid)
            {
                leftDevice.TryGetFeatureValue(CommonUsages.grip, out leftGripValue);
            }
        }

        if (rightController != null)
        {
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
            {
                rightDevice.TryGetFeatureValue(CommonUsages.grip, out rightGripValue);
            }
        }
    }

    void UpdateHandAnimations()
    {
        if (!enableHandAnimations) return;

        // Animar mano izquierda
        if (leftHand != null)
        {
            float targetScale = Mathf.Lerp(1f, 0.8f, leftGripValue);
            Vector3 currentScale = leftHand.localScale;
            Vector3 targetScaleVec = leftHandOriginalScale * targetScale;
            leftHand.localScale = Vector3.Lerp(currentScale, targetScaleVec, Time.deltaTime * handGrabSpeed);
        }

        // Animar mano derecha
        if (rightHand != null)
        {
            float targetScale = Mathf.Lerp(1f, 0.8f, rightGripValue);
            Vector3 currentScale = rightHand.localScale;
            Vector3 targetScaleVec = rightHandOriginalScale * targetScale;
            rightHand.localScale = Vector3.Lerp(currentScale, targetScaleVec, Time.deltaTime * handGrabSpeed);
        }
    }

    // ====================================================================
    // CALLBACKS DE AGARRE (Llamados desde StickyNote)
    // ====================================================================
    public void OnGrabObject(Transform objectTransform)
    {
        if (!isLocalPlayer) return;

        // Determinar qué mano está más cerca del objeto
        float distLeft = leftHand != null ? Vector3.Distance(leftHand.position, objectTransform.position) : float.MaxValue;
        float distRight = rightHand != null ? Vector3.Distance(rightHand.position, objectTransform.position) : float.MaxValue;

        if (distLeft < distRight && distLeft < grabDetectionRadius)
        {
            // Mano izquierda agarra
            isLeftHandGrabbing = true;
            grabbedObjectLeft = objectTransform;

            if (showDebugInfo)
                Debug.Log($"👈 Mano izquierda agarró: {objectTransform.name}");
        }
        else if (distRight < grabDetectionRadius)
        {
            // Mano derecha agarra
            isRightHandGrabbing = true;
            grabbedObjectRight = objectTransform;

            if (showDebugInfo)
                Debug.Log($"👉 Mano derecha agarró: {objectTransform.name}");
        }

        // Sincronizar a través de RPC
        photonView.RPC("RPC_OnGrabObject", RpcTarget.Others, objectTransform.GetComponent<PhotonView>().ViewID);
    }

    public void OnReleaseObject()
    {
        if (!isLocalPlayer) return;

        // Soltar objetos de ambas manos
        if (isLeftHandGrabbing)
        {
            if (showDebugInfo && grabbedObjectLeft != null)
                Debug.Log($"👈 Mano izquierda soltó: {grabbedObjectLeft.name}");

            isLeftHandGrabbing = false;
            grabbedObjectLeft = null;
        }

        if (isRightHandGrabbing)
        {
            if (showDebugInfo && grabbedObjectRight != null)
                Debug.Log($"👉 Mano derecha soltó: {grabbedObjectRight.name}");

            isRightHandGrabbing = false;
            grabbedObjectRight = null;
        }

        // Sincronizar a través de RPC
        photonView.RPC("RPC_OnReleaseObject", RpcTarget.Others);
    }

    // ====================================================================
    // RPCs PARA SINCRONIZAR ANIMACIONES
    // ====================================================================
    [PunRPC]
    void RPC_OnGrabObject(int objectViewID)
    {
        // Otros jugadores ven la animación de agarre
        PhotonView objView = PhotonView.Find(objectViewID);
        if (objView != null && showDebugInfo)
        {
            Debug.Log($"📡 {photonView.Owner.NickName} agarró {objView.name}");
        }
    }

    [PunRPC]
    void RPC_OnReleaseObject()
    {
        // Otros jugadores ven que soltó el objeto
        if (showDebugInfo)
            Debug.Log($"📡 {photonView.Owner.NickName} soltó un objeto");
    }

    // ====================================================================
    // EXTRAS (Color y Callbacks)
    // ====================================================================
    void AssignPlayerColor()
    {
        int id = photonView.Owner.ActorNumber;
        Color c = playerColors[(id - 1) % playerColors.Length];
        SetColor(c);
    }

    void SetColor(Color c)
    {
        // Pintar todos los renderers del avatar
        foreach (var r in GetComponentsInChildren<MeshRenderer>())
        {
            r.material.color = c;
        }
    }

    // ====================================================================
    // SINCRONIZACIÓN DE RED
    // ====================================================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Enviar mis datos a la red
            stream.SendNext(head != null ? head.position : Vector3.zero);
            stream.SendNext(head != null ? head.rotation : Quaternion.identity);

            stream.SendNext(leftHand != null ? leftHand.position : Vector3.zero);
            stream.SendNext(leftHand != null ? leftHand.rotation : Quaternion.identity);

            stream.SendNext(rightHand != null ? rightHand.position : Vector3.zero);
            stream.SendNext(rightHand != null ? rightHand.rotation : Quaternion.identity);

            // ✅ NUEVO: Sincronizar valores de grip para animaciones
            stream.SendNext(leftGripValue);
            stream.SendNext(rightGripValue);
        }
        else
        {
            // Recibir datos de la red
            netHeadPos = (Vector3)stream.ReceiveNext();
            netHeadRot = (Quaternion)stream.ReceiveNext();

            netLHandPos = (Vector3)stream.ReceiveNext();
            netLHandRot = (Quaternion)stream.ReceiveNext();

            netRHandPos = (Vector3)stream.ReceiveNext();
            netRHandRot = (Quaternion)stream.ReceiveNext();

            // ✅ NUEVO: Recibir valores de grip
            leftGripValue = (float)stream.ReceiveNext();
            rightGripValue = (float)stream.ReceiveNext();
        }
    }

    // ====================================================================
    // DEBUG VISUAL
    // ====================================================================
    private void OnDrawGizmos()
    {
        if (!showHandGizmos || !isLocalPlayer) return;

        // Mostrar área de detección de agarre
        if (leftHand != null)
        {
            Gizmos.color = isLeftHandGrabbing ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(leftHand.position, grabDetectionRadius);
        }

        if (rightHand != null)
        {
            Gizmos.color = isRightHandGrabbing ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(rightHand.position, grabDetectionRadius);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 400, 300, 150));
        GUILayout.Label("=== Avatar Debug ===");
        GUILayout.Label($"Mano izquierda: {(isLeftHandGrabbing ? "Agarrando" : "Libre")}");
        GUILayout.Label($"Mano derecha: {(isRightHandGrabbing ? "Agarrando" : "Libre")}");
        GUILayout.Label($"Grip L: {leftGripValue:F2} | R: {rightGripValue:F2}");
        GUILayout.EndArea();
    }
}