using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class LoadingManager : MonoBehaviour
{

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TextMeshProUGUI loadingText;

    [SerializeField] public Slider loadingBar;

    private int downloadMaxProgress = 30;
    private int headingMaxProgress = 10;
    private int gpsMaxProgress = 10;
    private int terrainDataMaxProgress = 50;

    private float currentProgress = 0f;

    private void Update()
    {
        Debug.Log(currentProgress);
    }

    public void SetText(string text = "")
    {
        loadingText.text = text;
    }

    public void ResetProgress()
    {
        currentProgress = 0;
    }

    public void SetDownloadProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * downloadMaxProgress);
        
        if (percentage == 100)
        {
            currentProgress += downloadMaxProgress;
        }
    }

    public void SetTerrainDataProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * terrainDataMaxProgress);
        
        if (percentage == 100)
        {
            currentProgress += terrainDataMaxProgress;
        }
    }

    public void SetHeadingProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * headingMaxProgress);

        if (percentage == 100)
        {
            currentProgress += headingMaxProgress;
        }
    }

    public void SetGPSProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * gpsMaxProgress);

        if (percentage == 100)
        {
            currentProgress += gpsMaxProgress;
        }
    }

}
