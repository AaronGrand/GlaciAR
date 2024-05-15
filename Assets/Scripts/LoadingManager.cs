using UnityEngine.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the visual representation of the loading process within the application.
/// This class controls a loading screen, including a progress bar and text feedback, to inform the user of the current loading status.
/// </summary>
public class LoadingManager : MonoBehaviour
{

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TextMeshProUGUI loadingText;

    [SerializeField] public Slider loadingBar;

    private int downloadMaxProgress = 30;
    private int gpsMaxProgress = 20;
    private int terrainDataMaxProgress = 50;

    private float currentProgress = 0f;

    /// <summary>
    /// Sets the text of the loading screen to inform the user of the current process.
    /// </summary>
    /// <param name="text">The text to display, defaults to an empty string if none is provided.</param>
    public void SetText(string text = "")
    {
        loadingText.text = text;
    }

    /// <summary>
    /// Resets the loading progress to zero.
    /// </summary>
    public void ResetProgress()
    {
        currentProgress = 0;
    }

    /// <summary>
    /// Updates the download progress on the loading bar.
    /// </summary>
    /// <param name="percentage">The current progress percentage of the download.</param>
    public void SetDownloadProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * downloadMaxProgress);
        
        if (percentage == 100)
        {
            currentProgress += downloadMaxProgress;
        }
    }

    /// <summary>
    /// Updates the compass progress on the loading bar.
    /// </summary>
    /// <param name="percentage">The current progress percentage of the compass.</param>
    public void SetGPSProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * gpsMaxProgress);

        if (percentage == 100)
        {
            currentProgress += gpsMaxProgress;
        }
    }


    /// <summary>
    /// Updates the terrain data loading progress on the loading bar.
    /// </summary>
    /// <param name="percentage">The current progress percentage of the terrain data loading.</param>
    public void SetTerrainDataProgress(int percentage = 0)
    {
        loadingBar.value = currentProgress + ((float)percentage / 100f * terrainDataMaxProgress);
        
        if (percentage == 100)
        {
            currentProgress += terrainDataMaxProgress;
        }
    }
}
