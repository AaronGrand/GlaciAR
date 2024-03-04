using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class LoadingManager : MonoBehaviour
{

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TextMeshProUGUI loadingText;

    [SerializeField] public Slider loadingBar;

    [SerializeField] public int downloadMaxProgress = 30;

    public void SetDownloadProgress(int percentage = 0)
    {
        loadingBar.value = (float)percentage / 100f * downloadMaxProgress;
    }

    public void SetDownloadBarText(string text = "")
    {
        loadingText.text = text;
    }


}
