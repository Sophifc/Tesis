using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

/// <summary>
/// Hace que la mano del avatar pueda agarrar objetos
/// La mano se ESTIRA hacia objetos cercanos para agarrarlos
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class AvatarHandInteractor : MonoBehaviour
{
    [Header("Configuración")]
    public KeyCode grabKey = KeyCode.G;
    public float detectionRange = 0.5f; // Rango para detectar objetos
    public float grabReachDistance = 0.8f; // Distancia a la que puede estirar la mano
    public float handStretchSpeed = 15f; // Velocidad de estiramiento

    [Header("Feedback Visual")]
    public bool highlightGrabbable = true;
    public Color highlightColor = Color.green;

    [Header("Debug")]
    public bool showDebug = true;

    private XRGrabInteractable currentTarget = null;
    private XRGrabInteractable grabbedObject = null;
    private SphereCollider handCollider;
    private Vector3 originalHandPosition;
    private bool isStretching = false;
    private List<XRGrabInteractable> nearbyObjects = new List<XRGrabInteractable>();

    // Referencias
    private PlayerAvatarController avatarController;

    void Start()
    {
        handCollider = GetComponent<SphereCollider>();
        handCollider.isTrigger = true;
        handCollider.radius = detectionRange;

        avatarController = GetComponentInParent<PlayerAvatarController>();

        if (showDebug)
            Debug.Log($"✅ AvatarHandInteractor inicializado en {gameObject.name}");
    }

    void Update()
    {
        // Buscar el objeto más cercano
        UpdateClosestTarget();

        // Detectar input de agarre
        if (Input.GetKeyDown(grabKey))
        {
            if (grabbedObject == null && currentTarget != null)
            {
                // Agarrar
                TryGrab();
            }
            else if (grabbedObject != null)
            {
                // Soltar
                Release();
            }
        }

        // Si está agarrando, mantener el objeto en la mano
        if (grabbedObject != null && !isStretching)
        {
            grabbedObject.transform.position = transform.position;
        }
    }

    void UpdateClosestTarget()
    {
        // Limpiar lista de nulos
        nearbyObjects.RemoveAll(x => x == null);

        if (nearbyObjects.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // Encontrar el más cercano
        XRGrabInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (var obj in nearbyObjects)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < closestDist && dist <= grabReachDistance)
            {
                closestDist = dist;
                closest = obj;
            }
        }

        currentTarget = closest;
    }

    void OnTriggerEnter(Collider other)
    {
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();

        if (grabInteractable != null && !nearbyObjects.Contains(grabInteractable))
        {
            nearbyObjects.Add(grabInteractable);

            // Highlight visual
            if (highlightGrabbable)
            {
                HighlightObject(grabInteractable, true);
            }

            if (showDebug)
                Debug.Log($"👋 Objeto cercano: {other.gameObject.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();

        if (grabInteractable != null && nearbyObjects.Contains(grabInteractable))
        {
            nearbyObjects.Remove(grabInteractable);

            // Quitar highlight
            if (highlightGrabbable)
            {
                HighlightObject(grabInteractable, false);
            }
        }
    }

    void TryGrab()
    {
        if (currentTarget == null) return;

        grabbedObject = currentTarget;

        // Notificar al avatar que está agarrando
        if (avatarController != null)
        {
            avatarController.OnGrabObject(grabbedObject.transform);
        }

        // Estirar la mano hacia el objeto
        StartCoroutine(StretchToGrab());

        if (showDebug)
            Debug.Log($"✋ Agarrando: {grabbedObject.gameObject.name}");
    }

    System.Collections.IEnumerator StretchToGrab()
    {
        isStretching = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = grabbedObject.transform.position;
        float elapsed = 0f;
        float duration = 0.15f;

        // Estirar mano hacia el objeto
        while (elapsed < duration && grabbedObject != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        if (grabbedObject != null)
        {
            // Desactivar física
            Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Desparentar de la pizarra si estaba pegado
            grabbedObject.transform.SetParent(transform);

            // Quitar highlight
            if (highlightGrabbable)
            {
                HighlightObject(grabbedObject, false);
            }
        }

        isStretching = false;
    }

    void Release()
    {
        if (grabbedObject == null) return;

        // Desparentar
        grabbedObject.transform.SetParent(null);

        // Reactivar física
        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
        }

        // Notificar al avatar
        if (avatarController != null)
        {
            avatarController.OnReleaseObject();
        }

        if (showDebug)
            Debug.Log($"👋 Soltado: {grabbedObject.gameObject.name}");

        grabbedObject = null;
    }

    void HighlightObject(XRGrabInteractable obj, bool highlight)
    {
        if (obj == null) return;

        MeshRenderer renderer = obj.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                // Añadir un brillo sutil
                renderer.material.SetColor("_EmissionColor", highlightColor * 0.3f);
                renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                // Quitar brillo
                renderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Visualizar rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualizar rango de agarre
        Gizmos.color = currentTarget != null ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, grabReachDistance);

        // Línea hacia el objeto más cercano
        if (currentTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }

    public void OnGrabObject(Transform objTransform)
    {
        // Dejar vacío
    }

    public void OnReleaseObject()
    {
        // Dejar vacío
    }
}