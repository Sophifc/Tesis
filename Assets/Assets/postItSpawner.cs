using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PostItSpawner : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Transform parentDispenser;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Busca el "padre" (el dispenser) para saber dónde generar el próximo
        parentDispenser = transform.parent;
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Si hay un dispenser (el padre que tiene el script PostItDispenser)
        PostItDispenser dispenser = parentDispenser.GetComponent<PostItDispenser>();
        if (dispenser != null)
        {
            dispenser.SpawnNewNote(); // Genera otro en la misma posición
        }
    }
}
