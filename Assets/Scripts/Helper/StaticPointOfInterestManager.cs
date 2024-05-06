using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticPointOfInterestManager
{
    private static CoroutineRunner coroutineRunner;
    private static PointOfInterestManager manager;

    static StaticPointOfInterestManager()
    {
        GameObject go = new GameObject("CoroutineRunner");
        coroutineRunner = go.AddComponent<CoroutineRunner>();
        GameObject managerGO = new GameObject("PointOfInterestManagerInstance");
        manager = managerGO.AddComponent<PointOfInterestManager>();
        Object.DontDestroyOnLoad(go);  // Prevent destruction between scenes
        Object.DontDestroyOnLoad(managerGO);
    }

    public static void LoadLocalizedMountains(string fileName, Transform parent)
    {
        coroutineRunner.StartCoroutine(manager.LoadLocalizedMountainsCoroutine(fileName, parent));
    }
}

// Utility MonoBehaviour class to handle coroutines
public class CoroutineRunner : MonoBehaviour
{
}