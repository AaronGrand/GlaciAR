using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class PointOfInterestManager : MonoBehaviour
{

    private static List<Mountain> mountains = new List<Mountain>();

    public static void LoadLocalizedMountains(string fileName, Transform parent)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            MountainData loadedData = JsonUtility.FromJson<MountainData>(dataAsJson);

            mountains.Clear();
            mountains.AddRange(loadedData.mountains);

            foreach (var mountain in loadedData.mountains)
            {
                Vector3 position = ConvertLatLonToPosition(mountain.latitude, mountain.longitude, GPS.Instance.activeGlacier);

                CreateMountainMarker(position, mountain.name, parent);
            }
        }
        else
        {
            Debug.LogError("Cannot find file!");
        }
    }

    private static Vector3 ConvertLatLonToPosition(float lat, float lon, Glacier activeGlacier)
    {
        Vector2 unityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(activeGlacier.centerPosition, new GpsData(lat, lon, 0.0d));
        // adjust height
        return GPS.Instance.CalculatePositionOnMesh(new Vector3((float)(unityLocation.x), 0.0f, (float)(unityLocation.y)));
    }

    private static void CreateMountainMarker(Vector3 position, string name, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.parent = parent;

        textObj.AddComponent<PointOfInterest>();
        textObj.transform.parent = parent;
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = name;
        textMesh.fontSize = 20;
        textObj.transform.position = position;
        textMesh.alignment = TextAlignmentOptions.Baseline;

        // Scale it and adjust the height
        float distance = Vector3.Distance(position, GPS.Instance.Camera.position);
        Debug.Log(name + " " + distance);
        textObj.transform.localScale = Vector3.one * (distance * 0.01f);
        textObj.transform.position = position + new Vector3(0.0f, distance * 0.05f, 0.0f);
    }
}

[System.Serializable]
public class Mountain
{
    public string name;
    public float latitude;
    public float longitude;
}

[System.Serializable]
public class MountainData
{
    public Mountain[] mountains;
}