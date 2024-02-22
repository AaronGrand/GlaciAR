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

    public void OnLoad(int index)
    {
        /*if (index == 0)
        {
            GPS.Instance.enabled = false;
        } else
        {
            GPS.Instance.enabled = true;
        }*/
        LoadGlacier();
        SceneManager.LoadSceneAsync(index);

        GPS.Instance.StartLoadingTerrain();
        
        //StartCoroutine(LoadSceneCoroutine(index));
    }

    IEnumerator LoadSceneCoroutine(int index)
    {
        //LoadGlacier();

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(index);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (GPS.Instance.started)
        {
            GPS.Instance.StartLoadingTerrain();
        }

        // Unload the scene
        SceneManager.UnloadSceneAsync(index);
    }

    private void LoadGlacier()
    {
        loadingScreen.SetActive(true);
        startScreen.SetActive(false);
    }

}
