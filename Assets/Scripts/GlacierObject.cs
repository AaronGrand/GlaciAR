using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlacierObject : MonoBehaviour
{
    public GameObject[] glacierStates;
    public int activeGlacierIndex;

    public GameObject glacierBed;
    public GameObject terrain;

    public void SetAllActive(bool active)
    {
        foreach(GameObject glacier in glacierStates)
        {
            glacier.SetActive(active);
        }
    }

    public void SetGlacier(int index)
    {
        SetAllActive(false);
        glacierStates[index].SetActive(true);
        GPS.Instance.sceneSelector.OnChangeGlacier(glacierStates[index].name.Substring(2, 4));
    }

    public void SetMaterial(Material glacierMaterial)
    {
        foreach (GameObject glacier in glacierStates)
        {
            glacier.GetComponent<Renderer>().material = glacierMaterial;
        }
    }
}
