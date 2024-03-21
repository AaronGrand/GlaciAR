using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneSelector : MonoBehaviour
{
    [SerializeField] private GameObject UI_glacierSelection;
    [SerializeField] private GameObject UI_simulationSelection;
    [SerializeField] private GameObject UI_loadingScreen;
    public GameObject UI_glaciAR;
    public Slider glaciARSlider;
    public TextMeshProUGUI glaciARSliderText;

    private void Start()
    {
        OnMenuClicked();
    }

    public void OnMenuClicked()
    {
        UI_glacierSelection.SetActive(true);
        UI_simulationSelection.SetActive(false);
        UI_loadingScreen.SetActive(false);
        UI_glaciAR.SetActive(false);

        // Destroy all temp Elements
        GPS.Instance.ResetGlacier();
    }

    public void OnGlacierSelect(int index)
    {
        GPS.Instance.activeGlacier = GPS.Instance.glaciers[index];

        UI_simulationSelection.SetActive(true);

        UI_glacierSelection.SetActive(false);
        UI_loadingScreen.SetActive(false);
        UI_glaciAR.SetActive(false);
    }

    public void OnSimulationSelect(bool simulateGPS)
    {
        GPS.Instance.loadingManager.ResetProgress();
        LoadGlacierUI();

        GPS.Instance.StartLoadingTerrain(simulateGPS);
    }

    private void LoadGlacierUI()
    {
        UI_loadingScreen.SetActive(true);

        UI_glacierSelection.SetActive(false);
        UI_simulationSelection.SetActive(false);
        UI_glaciAR.SetActive(false);
    }

    public void LoadingDoneUI()
    {
        UI_glaciAR.SetActive(true);

        UI_glacierSelection.SetActive(false);
        UI_simulationSelection.SetActive(false);
        UI_loadingScreen.SetActive(false);
    }

    public void OnChangeGlacier(string year)
    {
        glaciARSliderText.text = year;
    }
}
