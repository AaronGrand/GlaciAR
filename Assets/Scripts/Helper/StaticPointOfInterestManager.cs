using UnityEngine;

/// <summary>
/// Provides a static interface for managing and loading points of interest, such as mountains.
/// This class ensures that points of interest can be managed globally without attaching the manager directly to a GameObject that may be destroyed.
/// </summary>
public static class StaticPointOfInterestManager
{
    private static CoroutineRunner coroutineRunner;
    private static PointOfInterestManager manager;

    /// <summary>
    /// Static constructor to create and initialize the CoroutineRunner and PointOfInterestManager components.
    /// These components are attached to newly created GameObjects that persist across scene loads.
    /// </summary>
    static StaticPointOfInterestManager()
    {
        GameObject go = new GameObject("CoroutineRunner");
        coroutineRunner = go.AddComponent<CoroutineRunner>();
        GameObject managerGO = new GameObject("PointOfInterestManagerInstance");
        manager = managerGO.AddComponent<PointOfInterestManager>();
        Object.DontDestroyOnLoad(go);
        Object.DontDestroyOnLoad(managerGO);
    }

    /// <summary>
    /// Initiates the loading of localized mountains data from a file and instantiates points of interest within the scene.
    /// </summary>
    /// <param name="fileName">The name of the file containing the mountain data in JSON format.</param>
    /// <param name="parent">The parent transform under which the mountain points of interest should be instantiated.</param>
    public static void LoadLocalizedMountains(string fileName, Transform parent)
    {
        coroutineRunner.StartCoroutine(manager.LoadLocalizedMountainsCoroutine(fileName, parent));
    }
}

/// <summary>
/// Utility MonoBehaviour class used solely to run coroutines from a static context.
/// </summary>
public class CoroutineRunner : MonoBehaviour { }