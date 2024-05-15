using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Represents a glacier within the virtual environment, containing all necessary data for its visualization and interaction.
/// This class is used as a ScriptableObject to allow easy management and instantiation within Unity's editor.
/// </summary>
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

    public string pointOfInterestFileName;
}
