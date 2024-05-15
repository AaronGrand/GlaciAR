using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the display of localized text within a UI element using TextMeshPro.
/// This component should be attached to all UI objects that need will be translated based on a json.
/// </summary>
public class SetUIText : MonoBehaviour, ILocalizable
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private string name_in_language_json;

    /// <summary>
    /// Initializes the component by setting the text field based on localization.
    /// Subscribes to language changes to ensure the text updates dynamically.
    /// </summary>
    private void Start()
    {
        if (text == null || string.IsNullOrEmpty(name_in_language_json))
        {
            Debug.LogError("Missing references in inspector!");
            return;
        }

        LanguageTextManager.Subscribe(this);
        InitializeText();
    }

    /// <summary>
    /// Unsubscribes from language change notifications when the component is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        LanguageTextManager.Unsubscribe(this);
    }

    /// <summary>
    /// Called when the language is changed, triggering an update of the text based on the new settings.
    /// </summary>
    public void OnLocalizationChanged()
    {
        InitializeText();
    }

    /// <summary>
    /// Sets the text of the TextMeshProUGUI component to a localized value based on the provided JSON key.
    /// </summary>
    public void InitializeText()
    {
        text.text = LanguageTextManager.GetLocalizedValue(name_in_language_json);
    }
}