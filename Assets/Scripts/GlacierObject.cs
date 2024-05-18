using UnityEngine;

/// <summary>
/// Manages the state and visualization of different glacier states in the scene.
/// This component allows switching between glacier representations, 
/// updating materials, and toggling outlines.
/// </summary>
public class GlacierObject : MonoBehaviour
{
    public GameObject[] glacierStates;
    public int activeGlacierIndex;

    public GameObject glacierBed;
    public GameObject terrain;

    public delegate void GlacierChangeHandler(string year);

    /// <summary>
    /// Sets the activation state of all glacier children GameObjects.
    /// </summary>
    /// <param name="active">The active state to set for all glacier children GameObjects.</param>
    public void SetActiveForAllGlaciers(bool active)
    {
        foreach (GameObject glacier in glacierStates)
        {
            glacier.SetActive(active);
        }
    }

    /// <summary>
    /// Activates a specific glacier state based on the given index.
    /// Deactivates all other glacier states and notifies the scene selector of the change.
    /// </summary>
    /// <param name="index">The index of the glacier state to activate.</param>
    public void SetGlacier(int index)
    {
        if (index < 0 || index >= glacierStates.Length)
        {
            Debug.LogError("Invalid glacier index: " + index);
            return;
        }

        
        SetActiveForAllGlaciers(false);
        glacierStates[index].SetActive(true);
        activeGlacierIndex = index;

        GPS.Instance.sceneSelector.OnChangeGlacier(glacierStates[index].name.Substring(2, 4));
    }

    /// <summary>
    /// Applies a specified material to all glacier GameObjects.
    /// </summary>
    /// <param name="glacierMaterial">The material to apply to the glacier states.</param>
    public void SetGlacierMaterial(Material glacierMaterial)
    {
        foreach (GameObject glacier in glacierStates)
        {
            var renderer = glacier.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = glacierMaterial;
            }
        }
    }

    /// <summary>
    /// Applies a specified material to the terrain GameObject.
    /// </summary>
    /// <param name="terrainMaterial">The material to apply to the terrain.</param>
    public void SetTerrainMaterial(Material terrainMaterial)
    {
        foreach (GameObject glacier in glacierStates)
        {
            var renderer = terrain.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = terrainMaterial;
            }
        }
    }

    /// <summary>
    /// Toggles the outline effect on the terrain.
    /// </summary>
    public void ToggleTerrainOutline()
    {
        Outline outlineScript = terrain.GetComponent<Outline>();
        if (outlineScript != null)
        {
            outlineScript.enabled = !outlineScript.enabled;
            // Update the GPS state to reflect the change accurately
            GPS.Instance.outline = outlineScript.enabled;
        }
        else
        {
            Debug.LogError("Outline script not found on terrain.");
        }
    }
}
