using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Samples.DeviceSimulator;

/// <summary>
/// Oculta/muestra los controles visuales del XR Device Simulator
/// Útil para grabar videos limpios
/// </summary>
public class SimulatorUIHider : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tecla para ocultar/mostrar controles (default: H)")]
    public KeyCode toggleKey = KeyCode.H;

    [Tooltip("Ocultar controles al iniciar")]
    public bool hideOnStart = false;

    private XRDeviceSimulator simulator;
    private Canvas simulatorCanvas;
    private bool isHidden = false;

    void Start()
    {
        // Buscar el Device Simulator
        simulator = FindObjectOfType<XRDeviceSimulator>();

        if (simulator != null)
        {
            // Buscar el Canvas del simulador (si existe)
            simulatorCanvas = simulator.GetComponentInChildren<Canvas>();

            if (hideOnStart)
            {
                HideControls();
            }

            Debug.Log($"✅ SimulatorUIHider inicializado. Presiona '{toggleKey}' para ocultar/mostrar controles.");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró XR Device Simulator. SimulatorUIHider desactivado.");
            enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleControls();
        }
    }

    public void ToggleControls()
    {
        if (isHidden)
        {
            ShowControls();
        }
        else
        {
            HideControls();
        }
    }

    public void HideControls()
    {
        if (simulator == null) return;

        // Ocultar el Canvas si existe
        if (simulatorCanvas != null)
        {
            simulatorCanvas.enabled = false;
        }

        // Ocultar los gizmos visuales (manos, rayos)
        // Esto depende de cómo esté configurado tu simulator
        // Puedes desactivar los MeshRenderers de las manos visuales

        MeshRenderer[] renderers = simulator.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        LineRenderer[] lines = simulator.GetComponentsInChildren<LineRenderer>();
        foreach (var line in lines)
        {
            line.enabled = false;
        }

        isHidden = true;
        Debug.Log("🙈 Controles del simulador OCULTOS (para grabación limpia)");
    }

    public void ShowControls()
    {
        if (simulator == null) return;

        if (simulatorCanvas != null)
        {
            simulatorCanvas.enabled = true;
        }

        MeshRenderer[] renderers = simulator.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        LineRenderer[] lines = simulator.GetComponentsInChildren<LineRenderer>();
        foreach (var line in lines)
        {
            line.enabled = true;
        }

        isHidden = false;
        Debug.Log("👁️ Controles del simulador VISIBLES");
    }
}
