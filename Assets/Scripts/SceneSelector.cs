using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSelector : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject startScreen;

    private void Start()
    {
        loadingScreen.SetActive(false);
        startScreen.SetActive(true);
    }

    public void OnLoad(bool simulateGPS)
    {
        /*if (index == 0)
        {
            GPS.Instance.enabled = false;
        } else
        {
            GPS.Instance.enabled = true;
        }*/
        LoadGlacierUI();

        GPS.Instance.StartLoadingTerrain(simulateGPS);
    }

    private void LoadGlacierUI()
    {
        loadingScreen.SetActive(true);
        startScreen.SetActive(false);
    }

    public void LoadingDoneUI()
    {
        loadingScreen.SetActive(false);
        startScreen.SetActive(false);
    }

}
