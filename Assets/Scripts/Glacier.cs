using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Glacier", menuName = "Glaciers/Glacier")]
public class Glacier : ScriptableObject
{
    public string glacierName;
    public GpsData centerPosition;
    public int elevation;

    public double south;
    public double north;
    public double west;
    public double east;

    public int meshRangeInMeters;
    public GameObject glacier; // Reference to an FBX model prefab
    public Vector3 position; // Vector3 representing the glacier's rotation
    public Vector3 rotation; // Vector3 representing the glacier's rotation
    public Vector3 scaling; // Vector3 representing the glacier's aspect ratio

    public GameObject terrain;
    public GameObject glacierBed;

    public double scalingFactor;
}
