using UnityEngine;
using TMPro;
using System.Collections;

public class SetUIText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private string name_in_language_json;

    private void Start()
    {
        if (text == null || string.IsNullOrEmpty(name_in_language_json))
        {
            Debug.LogError("Missing references in inspector!");
            return;
        }

        InitializeText();
    }

    private void InitializeText()
    {
        text.text = LanguageTextManager.GetLocalizedValue(name_in_language_json);
    }
}
