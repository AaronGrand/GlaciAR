using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class PointOfInterestManager : MonoBehaviour
{
    private static List<Mountain> mountains = new List<Mountain>();

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

    private static Vector3 ConvertLatLonToPosition(float lat, float lon, Glacier activeGlacier)
    {
        Vector2 unityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(activeGlacier.centerPosition, new GpsData(lat, lon, 0.0d));
        return GPS.Instance.CalculatePositionOnMesh(new Vector3((float)(unityLocation.x), 0.0f, (float)(unityLocation.y)));
    }

    private static void CreateMountainMarker(Vector3 position, string name, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.parent = parent;

        textObj.AddComponent<PointOfInterest>();
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = name;
        textMesh.fontSize = 20;
        textObj.transform.position = position;
        textMesh.alignment = TextAlignmentOptions.Baseline;

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
