using UnityEngine;
using Photon.Pun; // ¡Importante!
using System.IO; // Necesario para leer archivos
using TMPro;     // ¡Muy importante! Necesario para trabajar con TextMeshPro

public class RequirementLoader : MonoBehaviourPunCallbacks
{
    [Header("Referencias de la Escena")]
    public GameObject postItPrefab; // Arrastra aquí tu prefab de Post-it
    public Transform whiteboardParent; // Arrastra aquí el objeto de la pizarra
    //private PhotonView photonView;

    //[Header("Configuración de Carga")]
    //public string filePath = "C:\\Users\\Tokyotech PC\\Desktop\\importar.txt";

    [Header("Configuración de Distribución")]
    public int postItsPerRow = 5;
    public float spacingX = 0.2f;
    public float spacingY = 0.3f;
    public Vector2 startPosition = new Vector2(-5.0f, 5f);

    void Awake()
    {
        //photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        // Solo el MasterClient (el creador) puede iniciar la carga.
        if (PhotonNetwork.IsMasterClient)
        {
            // Verificamos si hay una ruta de archivo guardada
            if (!string.IsNullOrEmpty(NetworkManager.FilePathToLoad))
            {
                // Leemos el contenido del archivo
                string fileContent = ReadFileContent(NetworkManager.FilePathToLoad);
                if (fileContent != null)
                {
                    // Llamamos a un RPC para que TODOS ejecuten la carga
                    //photonView.RPC("Rpc_LoadRequirements", RpcTarget.All, fileContent);
                    photonView.RPC("Rpc_LoadRequirements", RpcTarget.AllBuffered, fileContent);
                }
            }
        }
    }

    private string ReadFileContent(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("Archivo no encontrado en la ruta: " + path);
            return null;
        }
    }

    [PunRPC]
    void Rpc_LoadRequirements(string content)
    {
        //if (!File.Exists(filePath))
        //{
        //    Debug.LogError("¡Archivo no encontrado en la ruta: " + filePath);
        //    return;
        //}

        //string[] requirements = File.ReadAllLines(filePath);

        //Debug.Log("ARCHIVO ENCONTRADO");

        Debug.Log("Recibiendo requerimientos para cargar...");
        string[] requirements = content.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < requirements.Length; i++)


        {
            string currentRequirement = requirements[i].Trim();
            Debug.Log("currentRequirement");
            Debug.Log(currentRequirement);// Usamos Trim() para eliminar espacios en blanco al inicio/final

            if (string.IsNullOrWhiteSpace(currentRequirement))
            {
                continue;
            }

            GameObject newPostIt = Instantiate(postItPrefab);
            newPostIt.transform.SetParent(whiteboardParent);
            newPostIt.transform.localScale = new Vector3(0.1f, 0.1f, 0.01f);

            // 5. ASIGNAR EL TEXTO AL POST-IT (USANDO TMP_InputField)
            // Buscamos el componente TMP_InputField en los hijos del objeto instanciado.
            TMP_InputField postItInputField = newPostIt.GetComponentInChildren<TMP_InputField>();
            

            if (postItInputField != null)
            {
                // Asignamos el texto del requerimiento al campo de texto del Input Field.
                postItInputField.text = currentRequirement;
                Debug.Log("escritura");
                Debug.Log(postItInputField.text);

                // (Opcional) Si solo quieres mostrar el texto y no permitir que se edite al cargar:
                // postItInputField.readOnly = true; // El usuario no podrá escribir en él.
            }
            else
            {
                Debug.LogWarning("No se encontró un componente TMP_InputField en el prefab del Post-it.");
            }

            
            // 6. CALCULAR LA POSICIÓN EN LA PIZARRA (ALGORITMO DE GRID)
            int row = i / postItsPerRow;
            int column = i % postItsPerRow;

            float posX = startPosition.x + (column * spacingX);
            float posY = startPosition.y - (row * spacingY);

            newPostIt.transform.localPosition = new Vector3(posX, posY, 0.9f);


            //script de StickNote para pegarlos
            StickyNote note = newPostIt.GetComponent<StickyNote>();
            if (note != null)
            {
                note.Stick(); // Le damos la orden de pegarse
            }
            else
            {
                Debug.LogWarning("El prefab del Post-it no tiene el script StickyNote.cs");
            }
        }

        Debug.Log("¡Carga completada! Se procesaron " + requirements.Length + " líneas.");
    }
}