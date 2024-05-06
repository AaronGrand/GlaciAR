using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LanguageTextManager : MonoBehaviour
{
    private static Dictionary<string, string> localizedText;

    public static IEnumerator LoadLocalizedText(string fileName)
    {
        // Debug logging and setup
        Debug.Log("Loading " + fileName);
        localizedText = new Dictionary<string, string>();
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_EDITOR
        // Editor-specific path loading
        Debug.Log("Loading JSON from UNITY_EDITOR");
        string dataAsJson = File.ReadAllText(filePath);
        ProcessLoadedData(dataAsJson);

#elif UNITY_ANDROID
    // Android-specific path loading
    Debug.Log("Loading JSON from UNITY_ANDROID");
    string androidPath = "jar:file://" + filePath;
    UnityWebRequest www = UnityWebRequest.Get(androidPath);
    yield return www.SendWebRequest(); // Coroutine waiting

    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
    {
        Debug.LogError("Error loading JSON: " + www.error);
    }
    else
    {
        string dataAsJson = www.downloadHandler.text;
        ProcessLoadedData(dataAsJson);
    }
#endif

        yield break;
    }


    private static void ProcessLoadedData(string dataAsJson)
    {
        LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(dataAsJson);
        for (int i = 0; i < loadedData.items.Length; i++)
        {
            localizedText.Add(loadedData.items[i].key, loadedData.items[i].value);
        }
    }


    public static string GetLocalizedValue(string key)
    {
        if (localizedText != null && localizedText.ContainsKey(key))
        {
            return localizedText[key];
        }
        return "Key not found";
    }
}

[System.Serializable]
public class LocalizationData
{
    public LocalizationItem[] items;
}

[System.Serializable]
public class LocalizationItem
{
    public string key;
    public string value;
}