using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using Photon.Pun;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class StickyNote : MonoBehaviour, IPunInstantiateMagicCallback
{
    // --- Referencias de Componentes ---
    [Header("Controles VR")]
    public bool enableVRControls = true;
    private XRGrabInteractable grabInteractable;
    private bool wasGrabbedLastFrame = false;
    private Rigidbody rb;
    private PhotonView photonView;
    private MeshRenderer meshRenderer;
    private Vector3 correctScale;

    [Header("Configuración de Edición")]
    public Color[] availableColors = new Color[] {
        new Color(1, 1, 0.8f),      // Amarillo
        new Color(1, 0.8f, 0.8f),   // Rosa
        new Color(0.8f, 1, 0.8f),   // Verde
        new Color(0.8f, 0.8f, 1)    // Azul
    };
    public float scaleStep = 0.2f;
    public float minScale = 0.5f;
    public float maxScale = 3.0f;

    [Header("Texto del Requerimiento")]
    public TextMeshPro requirementText;

    [Header("Detección de Pizarra")]
    public float pizarraDetectionDistance = 1.0f; // ✅ Aumentado a 1 metro
    public LayerMask pizarraLayer; // Asignar layer de pizarra en el inspector
    public float stickDistance = 0.5f; // ✅ Distancia máxima para pegar cuando sueltas

    [Header("Debug")]
    public bool showDebugMessages = true;

    // --- Variables Privadas ---
    private Transform nearbyPizarra = null;
    private int currentColorIndex = 0;
    private string requirementContent = "";
    private float currentScaleFactor = 1f;
    private bool isCurrentlyGrabbed = false;
    private bool isStuck = false;
    private float lastPizarraCheckTime = 0f;
    private const float pizarraCheckInterval = 0.1f; // Revisar cada 0.1 segundos

    // ====================================================================
    // INICIALIZACIÓN
    // ====================================================================
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (grabInteractable == null)
        {
            Debug.LogError("❌ XRGrabInteractable no encontrado!");
            return;
        }

        // Suscribirse a eventos
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        // Escala inicial
        correctScale = transform.localScale;

        // Configurar física inicial
        rb.useGravity = false;
        rb.isKinematic = true;

        Debug.Log($"✅ StickyNote inicializado: {gameObject.name}");
    }

    // ====================================================================
    // UPDATE - DETECTAR PIZARRA CERCANA Y CONTROLES VR
    // ====================================================================
    private void Update()
    {
        // Solo el dueño detecta pizarra cercana cuando está agarrado
        if (photonView.IsMine && isCurrentlyGrabbed)
        {
            // Optimización: no revisar cada frame, sino cada 0.1s
            if (Time.time - lastPizarraCheckTime > pizarraCheckInterval)
            {
                DetectNearbyPizarra();
                lastPizarraCheckTime = Time.time;
            }

            // ✅ NUEVO: Procesar controles VR
            if (enableVRControls)
            {
                ProcessVRInput();
            }
        }
    }

    // ====================================================================
    // PROCESAR INPUT VR
    // ====================================================================
    void ProcessVRInput()
    {
        // Detectar qué mano está agarrando el post-it
        if (!grabInteractable.isSelected) return;

        var interactor = grabInteractable.firstInteractorSelecting;
        if (interactor == null) return;

        // Obtener el controlador
        XRController controller = interactor.transform.GetComponent<XRController>();
        if (controller == null) return;

        // Determinar qué mano es (Left o Right)
        InputDevice device = InputDevices.GetDeviceAtXRNode(controller.controllerNode);
        if (!device.isValid) return;

        // TRIGGER = Cambiar Color
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
        {
            if (!triggerWasPressed) // Solo una vez por presión
            {
                NextColor();
                triggerWasPressed = true;
            }
        }
        else
        {
            triggerWasPressed = false;
        }

        // BOTÓN PRIMARY (A en Quest derecha, X en Quest izquierda) = Aumentar tamaño
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
        {
            if (!primaryWasPressed)
            {
                IncreaseScale();
                primaryWasPressed = true;
            }
        }
        else
        {
            primaryWasPressed = false;
        }

        // BOTÓN SECONDARY (B en Quest derecha, Y en Quest izquierda) = Reducir tamaño
        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed)
        {
            if (!secondaryWasPressed)
            {
                DecreaseScale();
                secondaryWasPressed = true;
            }
        }
        else
        {
            secondaryWasPressed = false;
        }
    }

    // Variables para detectar presiones únicas
    private bool triggerWasPressed = false;
    private bool primaryWasPressed = false;
    private bool secondaryWasPressed = false;

    // ====================================================================
    // DETECCIÓN DE PIZARRA CERCANA
    // ====================================================================
    private void DetectNearbyPizarra()
    {
        // Buscar pizarras cercanas usando OverlapSphere
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, pizarraDetectionDistance);

        nearbyPizarra = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag("Pizarra"))
            {
                float distance = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearbyPizarra = col.transform;
                }
            }
        }

        if (showDebugMessages)
        {
            if (nearbyPizarra != null)
            {
                Debug.Log($"🎯 Pizarra detectada: {nearbyPizarra.name} a {closestDistance:F2}m");
            }
        }
    }

    // ====================================================================
    // BUSCAR PIZARRA AL SOLTAR (método más agresivo)
    // ====================================================================
    private Transform FindClosestPizarra()
    {
        // Buscar en un radio mayor cuando soltamos
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, stickDistance);

        Transform closestPizarra = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag("Pizarra"))
            {
                float distance = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPizarra = col.transform;
                }
            }
        }

        if (closestPizarra != null)
        {
            Debug.Log($"✅ Pizarra encontrada al soltar: {closestPizarra.name} a {closestDistance:F2}m");
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró pizarra cerca. Buscadas en radio de {stickDistance}m");
        }

        return closestPizarra;
    }

    // ====================================================================
    // FUNCIONES PÚBLICAS PARA CONTROLES VR
    // ====================================================================
    public void IncreaseScale()
    {
        if (!photonView.IsMine || !isCurrentlyGrabbed) return;

        currentScaleFactor += scaleStep;
        currentScaleFactor = Mathf.Clamp(currentScaleFactor, minScale, maxScale);

        ApplyScale();

        // Sincronizar en red
        photonView.RPC("SyncScale", RpcTarget.OthersBuffered, currentScaleFactor);

        Debug.Log($"🔼 Escala aumentada: factor={currentScaleFactor:F2}");
    }

    public void DecreaseScale()
    {
        if (!photonView.IsMine || !isCurrentlyGrabbed) return;

        currentScaleFactor -= scaleStep;
        currentScaleFactor = Mathf.Clamp(currentScaleFactor, minScale, maxScale);

        ApplyScale();

        // Sincronizar en red
        photonView.RPC("SyncScale", RpcTarget.OthersBuffered, currentScaleFactor);

        Debug.Log($"🔽 Escala reducida: factor={currentScaleFactor:F2}");
    }

    public void NextColor()
    {
        if (!photonView.IsMine || !isCurrentlyGrabbed) return;

        ChangeColor(1);
    }

    public void PreviousColor()
    {
        if (!photonView.IsMine || !isCurrentlyGrabbed) return;

        ChangeColor(-1);
    }

    // ====================================================================
    // APLICAR ESCALA
    // ====================================================================
    private void ApplyScale()
    {
        Vector3 newScale = new Vector3(
            correctScale.x * currentScaleFactor,
            correctScale.y * currentScaleFactor,
            correctScale.z // Mantener grosor Z original
        );

        transform.localScale = newScale;

        if (showDebugMessages)
        {
            Debug.Log($"✅ Escala aplicada: {newScale}, factor: {currentScaleFactor:F2}");
        }
    }

    // ====================================================================
    // CAMBIAR COLOR
    // ====================================================================
    private void ChangeColor(int direction)
    {
        currentColorIndex = (currentColorIndex + direction + availableColors.Length) % availableColors.Length;

        Color newColor = availableColors[currentColorIndex];
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
            Debug.Log($"🎨 Color cambiado a índice {currentColorIndex}: {newColor}");
        }

        // Sincronizar en red
        photonView.RPC("SyncColor", RpcTarget.OthersBuffered, currentColorIndex);
    }

    // ====================================================================
    // EVENTOS XR INTERACTION - GRAB
    // ====================================================================
    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log($"✋ POST-IT AGARRADO por {PhotonNetwork.NickName}");

        isCurrentlyGrabbed = true;
        isStuck = false;

        // Tomar ownership
        if (!photonView.IsMine)
        {
            photonView.RequestOwnership();
        }

        // Activar física mientras se agarra
        rb.isKinematic = false;
        rb.useGravity = false; // Sin gravedad mientras se agarra

        // Guardar escala mundial actual
        Vector3 currentWorldScale = transform.lossyScale;

        // Desparentar
        transform.SetParent(null);

        // Mantener la escala visual
        transform.localScale = currentWorldScale;

        // Actualizar correctScale
        correctScale = currentWorldScale / currentScaleFactor;

        // Sincronizar estado de agarrado
        photonView.RPC("RPCSetGrabbed", RpcTarget.AllBuffered, true, PhotonNetwork.NickName);

        // Notificar al avatar
        NotifyAvatarGrab(true);
    }

    // ====================================================================
    // EVENTOS XR INTERACTION - RELEASE
    // ====================================================================
    private void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log($"👋 POST-IT SOLTADO por {PhotonNetwork.NickName}");
        Debug.Log($"   Posición actual: {transform.position}");

        isCurrentlyGrabbed = false;

        // ✅ MÉTODO 1: Revisar la detección continua
        if (nearbyPizarra != null)
        {
            Debug.Log($"📌 Método 1: Pegando a pizarra detectada continuamente: {nearbyPizarra.name}");
            Stick(nearbyPizarra);
        }
        else
        {
            // ✅ MÉTODO 2: Hacer una búsqueda agresiva al soltar
            Debug.Log("🔍 Método 1 falló, buscando pizarra activamente...");
            Transform foundPizarra = FindClosestPizarra();

            if (foundPizarra != null)
            {
                Debug.Log($"📌 Método 2: Pegando a pizarra encontrada: {foundPizarra.name}");
                Stick(foundPizarra);
            }
            else
            {
                // ✅ MÉTODO 3: Si nada funciona, buscar TODAS las pizarras en la escena
                Debug.Log("🔍 Método 2 falló, buscando TODAS las pizarras en escena...");
                GameObject[] allPizarras = GameObject.FindGameObjectsWithTag("Pizarra");

                if (allPizarras.Length > 0)
                {
                    // Encontrar la más cercana
                    Transform closestPizarra = null;
                    float closestDist = stickDistance;

                    foreach (GameObject pizarra in allPizarras)
                    {
                        float dist = Vector3.Distance(transform.position, pizarra.transform.position);
                        Debug.Log($"   - Pizarra '{pizarra.name}' a {dist:F2}m");

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestPizarra = pizarra.transform;
                        }
                    }

                    if (closestPizarra != null)
                    {
                        Debug.Log($"📌 Método 3: Pegando a pizarra más cercana: {closestPizarra.name} ({closestDist:F2}m)");
                        Stick(closestPizarra);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Pizarras encontradas pero todas están a más de {stickDistance}m");
                        ActivateGravity();
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ NO SE ENCONTRARON PIZARRAS CON TAG 'Pizarra' en la escena");
                    ActivateGravity();
                }
            }
        }

        // Sincronizar estado de soltado
        photonView.RPC("RPCSetGrabbed", RpcTarget.AllBuffered, false, "");

        // Notificar al avatar
        NotifyAvatarGrab(false);
    }

    // ====================================================================
    // ACTIVAR GRAVEDAD (cuando no hay pizarra)
    // ====================================================================
    private void ActivateGravity()
    {
        Debug.Log("💨 Activando gravedad - el post-it caerá");

        rb.isKinematic = false;
        rb.useGravity = true;

        // Aplicar una pequeña velocidad inicial para que caiga
        rb.velocity = Vector3.down * 0.5f;
    }

    // ====================================================================
    // PEGAR A PIZARRA
    // ====================================================================
    public void Stick(Transform parentSurface)
    {
        // Guardar escala mundial ANTES de parentar
        Vector3 currentWorldScale = transform.lossyScale;

        Debug.Log($"📌 Pegando post-it a {parentSurface.name}");

        // ✅ NUEVO: Mover el post-it MÁS CERCA de la pizarra
        Collider pizarraCollider = parentSurface.GetComponent<Collider>();
        if (pizarraCollider != null)
        {
            // Encontrar el punto más cercano en la pizarra
            Vector3 closestPoint = pizarraCollider.ClosestPoint(transform.position);

            // Calcular la normal de la superficie (dirección hacia afuera)
            Vector3 directionToPizarra = (closestPoint - transform.position).normalized;

            // Posicionar el post-it muy cerca de la superficie (0.01m de separación)
            float offset = 0.01f;
            transform.position = closestPoint - directionToPizarra * offset;

            // Orientar el post-it paralelo a la pizarra
            // Si la pizarra tiene una rotación, copiarla
            transform.rotation = parentSurface.rotation;

            Debug.Log($"✅ Post-it reposicionado cerca de la pizarra");
            Debug.Log($"   Punto más cercano: {closestPoint}");
            Debug.Log($"   Nueva posición: {transform.position}");
        }

        // Desactivar física
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isStuck = true;

        // Parentar a la pizarra
        transform.SetParent(parentSurface);

        // Recalcular escala local para mantener la escala mundial
        Vector3 parentScale = parentSurface.lossyScale;
        Vector3 newLocalScale = new Vector3(
            currentWorldScale.x / parentScale.x,
            currentWorldScale.y / parentScale.y,
            currentWorldScale.z / parentScale.z
        );

        transform.localScale = newLocalScale;

        Debug.Log($"✅ Post-it pegado. Escala local: {transform.localScale}");

        // Sincronizar en red
        if (photonView.IsMine)
        {
            // Enviar tanto el ViewID como la posición/rotación/escala actual
            photonView.RPC("RPCStick", RpcTarget.OthersBuffered,
                parentSurface.GetComponent<PhotonView>().ViewID,
                transform.localPosition,
                transform.localRotation,
                transform.localScale);
        }
    }

    // ====================================================================
    // RPCs DE SINCRONIZACIÓN
    // ====================================================================
    [PunRPC]
    void SyncScale(float scaleFactor)
    {
        currentScaleFactor = scaleFactor;

        // Guardamos parent
        Transform originalParent = transform.parent;

        // Desparentamos temporalmente
        transform.SetParent(null);

        // Aplicamos escala
        Vector3 baseScale = correctScale;
        Vector3 newScale = new Vector3(
            baseScale.x * scaleFactor,
            baseScale.y * scaleFactor,
            baseScale.z
        );
        transform.localScale = newScale;

        // Re-parentamos
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
        }

        Debug.Log($"📡 Escala sincronizada: factor={scaleFactor:F2}");
    }

    [PunRPC]
    void SyncColor(int colorIndex)
    {
        currentColorIndex = colorIndex;
        if (meshRenderer != null)
        {
            meshRenderer.material.color = availableColors[currentColorIndex];
            Debug.Log($"📡 Color sincronizado: índice {colorIndex}");
        }
    }

    [PunRPC]
    void RPCSetGrabbed(bool isGrabbed, string grabberName)
    {
        if (isGrabbed)
        {
            Debug.Log($"📡 {grabberName} está agarrando este post-it");
        }
        else
        {
            Debug.Log($"📡 Post-it soltado");
        }
    }

    [PunRPC]
    void RPCStick(int pizarraViewID, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        PhotonView pizarraView = PhotonView.Find(pizarraViewID);
        if (pizarraView != null)
        {
            // Desactivar física
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            isStuck = true;

            // Parentar
            transform.SetParent(pizarraView.transform);

            // Aplicar transform sincronizado
            transform.localPosition = localPos;
            transform.localRotation = localRot;
            transform.localScale = localScale;

            Debug.Log($"📡 Post-it pegado remotamente a {pizarraView.name}");
            Debug.Log($"   Posición local: {localPos}");
            Debug.Log($"   Escala local: {localScale}");
        }
    }

    // ====================================================================
    // NOTIFICAR AL AVATAR
    // ====================================================================
    private void NotifyAvatarGrab(bool isGrabbing)
    {
        PlayerAvatarController[] avatars = FindObjectsOfType<PlayerAvatarController>();
        foreach (PlayerAvatarController avatarController in avatars)
        {
            if (avatarController != null && avatarController.photonView.IsMine)
            {
                if (isGrabbing)
                {
                    avatarController.OnGrabObject(transform);
                }
                else
                {
                    avatarController.OnReleaseObject();
                }
                break;
            }
        }
    }

    // ====================================================================
    // INICIALIZACIÓN PHOTON
    // ====================================================================
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;

        if (data != null && data.Length > 0)
        {
            // Recibir texto
            if (data[0] is string text)
            {
                requirementContent = text;

                TextMeshPro[] tmpComponents = GetComponentsInChildren<TextMeshPro>();
                if (tmpComponents.Length > 0)
                {
                    requirementText = tmpComponents[0];
                    requirementText.text = text;
                    Debug.Log($"✅ Texto asignado: '{text}'");
                }
            }

            // Recibir escala personalizada
            if (data.Length > 1 && data[1] is Vector3 scale)
            {
                transform.localScale = scale;
                correctScale = scale;
                currentScaleFactor = 1.0f;
                Debug.Log($"✅ Escala personalizada: {scale}");
            }
        }

        // Aplicar color inicial
        if (meshRenderer != null)
        {
            meshRenderer.material.color = availableColors[currentColorIndex];
        }

        // Buscar y pegar a pizarra inicial
        GameObject pizarra = GameObject.FindGameObjectWithTag("Pizarra");
        if (pizarra != null)
        {
            Stick(pizarra.transform);
        }

        Debug.Log($"📝 Post-it '{requirementContent}' creado");
    }

    // ====================================================================
    // LIMPIEZA
    // ====================================================================
    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    // ====================================================================
    // DEBUG - Visualizar área de detección
    // ====================================================================
    private void OnDrawGizmos()
    {
        // Mostrar área de detección continua (verde/amarillo)
        if (isCurrentlyGrabbed)
        {
            Gizmos.color = nearbyPizarra != null ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pizarraDetectionDistance);
        }

        // Mostrar área de stick (azul/rojo)
        Gizmos.color = isStuck ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, stickDistance);
    }
}