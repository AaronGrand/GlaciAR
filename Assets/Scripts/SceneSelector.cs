using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Manages user interactions with different UI panels for selecting glaciers, starting simulations, and handling loading screens.
/// This class controls the visibility of various UI components based on user actions, providing a seamless flow between different stages of the application.
/// </summary>
public class SceneSelector : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject UI_glacierSelection;
    [SerializeField] private GameObject UI_simulationSelection;
    [SerializeField] private GameObject UI_loadingScreen;
    [SerializeField] private GameObject UI_hamburger;
    [SerializeField] private GameObject UI_startScreen;

    [Header("Simulation UI")]
    public GameObject UI_glaciAR;
    public Slider glaciARSlider;
    public TextMeshProUGUI glaciARSliderText;
    public GameObject glaciARSliderHandleActivated;
    public TextMeshProUGUI glaciARSliderTextActivated;

    private Coroutine deactivateCoroutine;

    /// <summary>
    /// Initializes the UI panels and attaches event listeners on start.
    /// </summary>
    private void Start()
    {
        GPS.Instance.onLoadingComplete += LoadingDoneUI;
        StartCoroutine(LanguageTextManager.LoadLocalizedText("german.json"));
        UI_startScreen.SetActive(false);
        OnMenuClicked();
    }

    /// <summary>
    /// Configures the glacier timeline slider based on the number of glacier states available.
    /// </summary>
    /// <param name="maxStates">The maximum number of states the slider should handle.</param>
    /// <param name="onValueChanged">Action to perform when the slider value changes.</param>
    public void SetupSlider(int maxStates, Action<float> onValueChanged)
    {
        glaciARSlider.minValue = 0;
        glaciARSlider.maxValue = maxStates - 1;
        glaciARSlider.value = 0;
        glaciARSlider.onValueChanged.RemoveAllListeners(); // Clear previous listeners to prevent duplicates
        glaciARSlider.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<float>(onValueChanged));
    }

    /// <summary>
    /// Handles the initial menu button click, resetting the glacier and toggling the appropriate UI panels.
    /// </summary>
    public void OnMenuClicked()
    {
        ResetUI();
        GPS.Instance.ResetGlacier();
    }

    /// <summary>
    /// Handles selection of a specific glacier, triggering the loading of current glacier state and updating UI panels.
    /// </summary>
    /// <param name="index">The index of the selected glacier.</param>
    public void OnGlacierSelect(int index)
    {
        GPS.Instance.SetGlacier(index);
        ToggleUI(false, true, false, false, false);
    }

    /// <summary>
    /// Initiates the loading of the terrain based on user selection and updating UI panels.
    /// </summary>
    /// <param name="simulateGPS">Whether to simulate GPS data for the terrain loading.</param>
    public void OnSimulationSelect(bool simulateGPS)
    {
        GPS.Instance.loadingManager.ResetProgress();
        ToggleUI(false, false, true, false, false);
        GPS.Instance.StartLoadingTerrain(simulateGPS);
    }

    /// <summary>
    /// Toggles the visibility of various UI panels based on the current state of the application.
    /// </summary>
    /// <param name="glacierSelection">Whether the glacier selection panel should be visible.</param>
    /// <param name="simulationSelection">Whether the simulation selection panel should be visible.</param>
    /// <param name="loadingScreen">Whether the loading screen should be visible.</param>
    /// <param name="glaciAR">Whether the AR interface should be visible.</param>
    /// <param name="hamburger">Whether the hamburger menu should be visible.</param>
    private void ToggleUI(bool glacierSelection, bool simulationSelection, bool loadingScreen, bool glaciAR, bool hamburger)
    {
        UI_glacierSelection.SetActive(glacierSelection);
        UI_simulationSelection.SetActive(simulationSelection);
        UI_loadingScreen.SetActive(loadingScreen);
        UI_glaciAR.SetActive(glaciAR);
        UI_hamburger.SetActive(hamburger);
    }

    /// <summary>
    /// Updates UI panels to reflect that the loading process is complete.
    /// </summary>
    public void LoadingDoneUI()
    {
        ToggleUI(false, false, false, true, true);
    }

    /// <summary>
    /// Updates the glacier year display on the slider and temporarily activates the handle with year.
    /// </summary>
    /// <param name="year">The year to display as the current glacier state.</param>
    public void OnChangeGlacier(string year)
    {
        glaciARSliderText.text = year;
        glaciARSliderTextActivated.text = year;
        glaciARSliderHandleActivated.SetActive(true);

        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
        }
        deactivateCoroutine = StartCoroutine(DeactivateHandleAfterDelay(1.5f));
    }

    /// <summary>
    /// Coroutine to deactivate the slider's handle after a short delay.
    /// </summary>
    /// <param name="delay">The time in seconds to wait before deactivating the handle.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator DeactivateHandleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        glaciARSliderHandleActivated.SetActive(false);
    }
    /// <summary>
    /// Resets the UI to the initial state, typically used when restarting the scene or application.
    /// </summary>
    public void ResetUI()
    {
        ToggleUI(true, false, false, false, false);
    }
}
