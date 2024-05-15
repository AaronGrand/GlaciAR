using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExceptionHandler : MonoBehaviour
{
    public GameObject errorPanel;
    public TextMeshProUGUI errorMessageText;
    public Button okButton;

    private void Start()
    {
        // Ensure the panel is hidden initially
        errorPanel.SetActive(false);
    }

    // Call this method to display an error message
    public void ShowErrorMessage(string message)
    {
        errorMessageText.text = message;
        errorPanel.SetActive(true);
    }

    // Method to close the error panel
    public void CloseErrorPanel()
    {
        errorPanel.SetActive(false);
    }
}