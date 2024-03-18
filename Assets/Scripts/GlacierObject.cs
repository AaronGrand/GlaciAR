using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlacierObject : MonoBehaviour
{
    public GameObject[] glacierStates;
    public int activeGlacierIndex;

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
    }
}
