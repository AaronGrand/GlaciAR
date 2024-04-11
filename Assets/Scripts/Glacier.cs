using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
    public AssetReference glacier;
    
    public Material mat_terrain;
    public Material mat_glacierBed;
    public Material mat_glacier;
}
