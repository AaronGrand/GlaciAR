using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LanguageTextManager : MonoBehaviour
{
    private static Dictionary<string, string> localizedText;
    public static void LoadLocalizedText(string fileName)
    {
        Debug.Log("Loading " + fileName);
        localizedText = new Dictionary<string, string>();
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        //if (File.Exists(filePath))
        {
#if UNITY_EDITOR
            Debug.Log("Loading JSON from UNITY_EDITOR");
            string dataAsJson = File.ReadAllText(filePath);
#elif UNITY_ANDROID
            Debug.Log("Loading Json from UNITY_ANDROID");
            UnityWebRequest www = new UnityWebRequest(filePath);
            www.SendWebRequest();
            while (!www.isDone) { Debug.Log("freeze");} // Do nothing
            string dataAsJson = www.downloadHandler.text;
#endif
            LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(dataAsJson);

            for (int i = 0; i < loadedData.items.Length; i++)
            {
                localizedText.Add(loadedData.items[i].key, loadedData.items[i].value);
            }
            
        }
        //else
        /*{
            Debug.LogError("Cannot find file!");
        }*/
    }

    public static string GetLocalizedValue(string key)
    {
        string result = key;
        if (localizedText.ContainsKey(key))
        {
            result = localizedText[key];
        }
        return result;
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