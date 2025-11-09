using UnityEngine;
using Photon.Pun;
using System.IO;
using TMPro;

public class RequirementLoader : MonoBehaviourPunCallbacks
{
    [Header("Referencias de la Escena")]
    public GameObject postItPrefab; // Asegúrate de que este prefab esté en la carpeta "Resources"
    public Transform whiteboardParent;

    [Header("Configuración de Distribución")]
    public int postItsPerRow = 5;
    public float spacingX = 0.2f;
    public float spacingY = 0.3f;
    public Vector2 startPosition = new Vector2(-5.0f, 5f);
    public Vector3 postItScale = new Vector3(3f, 3f, 0.01f);
    public float postItZOffset = 0.9f;

    void Start()
    {
        // SOLO el Master Client (el creador de la sala) debe ejecutar esto.
        if (PhotonNetwork.IsMasterClient)
        {
            // Verificamos si hay una ruta de archivo guardada
            if (!string.IsNullOrEmpty(NetworkManager.FilePathToLoad))
            {
                string fileContent = ReadFileContent(NetworkManager.FilePathToLoad);
                if (fileContent != null)
                {
                    // ¡Ya no llamamos a un RPC! 
                    // El Master Client crea los objetos directamente.
                    InstantiateRequirements(fileContent);
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

    // Esta función ahora solo la ejecuta el Master Client
    void InstantiateRequirements(string content)
    {
        Debug.Log("Master Client cargando requerimientos...");
        string[] requirements = content.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < requirements.Length; i++)
        {
            string currentRequirement = requirements[i].Trim();

            // Tu filtro para ignorar líneas vacías o que no son requisitos
            if (string.IsNullOrWhiteSpace(currentRequirement))
            {
                continue;
            }

            // 1. Calculamos la posición LOCAL primero
            int row = i / postItsPerRow;
            int column = i % postItsPerRow;
            float posX = startPosition.x + (column * spacingX);
            float posY = startPosition.y - (row * spacingY);
            Vector3 localPos = new Vector3(posX, posY, postItZOffset);

            // 2. Convertimos la posición LOCAL a MUNDIAL (World Position)
            Vector3 spawnPosition = whiteboardParent.TransformPoint(localPos);
            //Quaternion spawnRotation = whiteboardParent.rotation; // Se alinea con la pizarra
            Quaternion spawnRotation = whiteboardParent.rotation * Quaternion.Euler(0, 180, 0);

            // 3. Preparamos los datos a enviar: el texto y la escala
            object[] instantiationData = new object[] { currentRequirement, postItScale };

            // 4. Instanciamos el Post-it POR RED
            // Photon se encargará de crearlo en todos los clientes
            PhotonNetwork.Instantiate(postItPrefab.name, spawnPosition, spawnRotation, 0, instantiationData);
        }
        Debug.Log("¡Carga completada por el Master Client!");
    }

}