using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

/// <summary>
/// Manages loading and displaying points of interest for mountains in the scene.
/// This class handles the reading of mountain data from a JSON file, creating visual markers for each mountain, and placing them appropriately based on their WGS-84 coordinates.
/// </summary>
public class PointOfInterestManager : MonoBehaviour
{
    private static List<Mountain> mountains = new List<Mountain>();

    /// <summary>
    /// Coroutine that loads mountain data from a JSON file, instantiates point of interest markers, and places them in the scene.
    /// </summary>
    /// <param name="fileName">The name of the file within the StreamingAssets directory that contains the JSON data.</param>
    /// <param name="parent">The parent transform under which mountain markers will be instantiated.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public IEnumerator LoadLocalizedMountainsCoroutine(string fileName, Transform parent)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        string dataAsJson = string.Empty;

#if UNITY_EDITOR
        if (File.Exists(filePath))
        {
            dataAsJson = File.ReadAllText(filePath);
        }
        else
        {
            Debug.LogError("Cannot find file in Editor: " + filePath);
            yield break;
        }

#elif UNITY_ANDROID
        string androidPath = "jar:file://" + filePath;
        UnityWebRequest www = UnityWebRequest.Get(androidPath);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error loading JSON on Android: " + www.error);
            yield break;
        }
        else
        {
            dataAsJson = www.downloadHandler.text;
        }
#endif

        MountainData loadedData = JsonUtility.FromJson<MountainData>(dataAsJson);

        mountains.Clear();
        mountains.AddRange(loadedData.mountains);

        foreach (var mountain in loadedData.mountains)
        {
            Vector3 position = ConvertLatLonToPosition(mountain.latitude, mountain.longitude, GPS.Instance.activeGlacier);
            CreateMountainMarker(position, mountain.name, parent);
        }
    }

    /// <summary>
    /// Converts latitude and longitude to a position in Unity's 3D space using the active glacier's location as a reference.
    /// </summary>
    /// <param name="lat">Latitude of the point.</param>
    /// <param name="lon">Longitude of the point.</param>
    /// <param name="activeGlacier">Reference to the currently active glacier.</param>
    /// <returns>3D position corresponding to the geographic coordinates.</returns>
    private static Vector3 ConvertLatLonToPosition(float lat, float lon, Glacier activeGlacier)
    {
        Vector2 unityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(activeGlacier.centerPosition, new GpsData(lat, lon, 0.0d));
        return GPS.Instance.CalculatePositionOnMesh(new Vector3((float)(unityLocation.x), 0.0f, (float)(unityLocation.y)));
    }

    /// <summary>
    /// Instantiates a new point of interest object for a mountain and sets its properties.
    /// </summary>
    /// <param name="position">Position where the marker should be placed.</param>
    /// <param name="name">Name of the mountain, which is displayed on the marker.</param>
    /// <param name="parent">The parent transform under which the marker will be instantiated.</param>
    private static void CreateMountainMarker(Vector3 position, string name, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.parent = parent;

        textObj.AddComponent<PointOfInterest>();
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.color = Color.black;
        textMesh.text = name;
        textMesh.fontSize = 20;
        textObj.transform.position = position;
        textMesh.alignment = TextAlignmentOptions.Baseline;

        float distance = Vector3.Distance(position, GPS.Instance.cameraOffset.transform.position);
        Debug.Log(name + " " + distance);
        textObj.transform.localScale = Vector3.one * (distance * 0.01f);
        textObj.transform.position = position + new Vector3(0.0f, distance * 0.05f, 0.0f);
    }
}