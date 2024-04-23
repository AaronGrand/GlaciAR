using UnityEngine;
using TMPro;

public class SetUIText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private string name_in_language_json;

    private void Start()
    {
        if(text == null || name_in_language_json == null)
        {
            Debug.Log("cannot find " + name_in_language_json);
            return;
        } else
        {
            text.text = LanguageTextManager.GetLocalizedValue(name_in_language_json);
        }
    }
}
