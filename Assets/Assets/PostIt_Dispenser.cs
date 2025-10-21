using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PostItDispenser : MonoBehaviour
{
    public GameObject postIt;
    public Transform spawn;

    private GameObject currentNote;

    void Start()
    {
        SpawnNewNote();
    }

    public void SpawnNewNote()
    {
        Debug.Log("Generando nuevo post-it...");
        currentNote = Instantiate(postIt, spawn.position, spawn.rotation, this.transform);

        var grab = currentNote.GetComponent<XRGrabInteractable>();
        //grab.selectEntered.AddListener(OnGrabbed);

        Debug.Log("Spawn local: " + spawn.localPosition + " | global: " + spawn.position);

        postIt.name = "PostIt_" + Time.time;
        Debug.Log("Nuevo Post-it instanciado en: " + spawn.position);
    }

//    void OnGrabbed(SelectEnterEventArgs args)
//    {
//        Invoke(nameof(SpawnNewNote), 0.5f);
//    }
}
