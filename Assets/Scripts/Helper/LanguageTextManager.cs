using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking; // Used for UNITY_ANDROID

/// <summary>
/// Manages the loading and accessing of localized text data, allowing dynamic text loading based on the selected language.
/// </summary>
public class LanguageTextManager : MonoBehaviour
{
    private static Dictionary<string, string> localizedText;
    private static List<ILocalizable> observers = new List<ILocalizable>();

    /// <summary>
    /// Subscribes a component to localization notifications.
    /// </summary>
    /// <param name="observer">The SET UI TEXT component that implements the ILocalizable interface and wishes to receive updates.</param>
    public static void Subscribe(ILocalizable observer)
    {
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
        }
    }

    /// <summary>
    /// Unsubscribes a component from localization notifications.
    /// </summary>
    /// <param name="observer">The component that no longer needs to receive localization updates.</param>
    public static void Unsubscribe(ILocalizable observer)
    {
        if (observers.Contains(observer))
        {
            observers.Remove(observer);
        }
    }

    /// <summary>
    /// Notifies all subscribed components that a localization change has occurred.
    /// </summary>
    private static void NotifyObservers()
    {
        foreach (var observer in observers)
        {
            observer.OnLocalizationChanged();
        }
    }

    /// <summary>
    /// Loads localized text data from a JSON file within the StreamingAssets directory.
    /// </summary>
    /// <param name="fileName">The file name of the JSON file containing the localized text.</param>
    /// <returns>An IEnumerator suitable for coroutine calls in Unity, handling asynchronous loading operations.</returns>
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
        NotifyObservers(); // Notify all observers after updating the data
        yield break;

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
        NotifyObservers(); // Notify all observers after updating the data
        yield break;
    }
#endif
        yield break;
    }

    /// <summary>
    /// Processes the loaded JSON data, populating the localized text dictionary.
    /// </summary>
    /// <param name="dataAsJson">The JSON string containing the localized text data.</param>
    private static void ProcessLoadedData(string dataAsJson)
    {
        TextLocalization loadedData = JsonUtility.FromJson<TextLocalization>(dataAsJson);
        for (int i = 0; i < loadedData.items.Length; i++)
        {
            localizedText.Add(loadedData.items[i].key, loadedData.items[i].value);
        }
    }

    /// <summary>
    /// Retrieves a localized string based on the provided key.
    /// </summary>
    /// <param name="key">The key for the localized text entry to retrieve.</param>
    /// <returns>The localized text associated with the key; returns "Key not found" if the key does not exist.</returns>
    public static string GetLocalizedValue(string key)
    {
        if (localizedText != null && localizedText.ContainsKey(key))
        {
            return localizedText[key];
        }
        return "Key not found";
    }
}