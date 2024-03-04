using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The Main Class of the Tool.
/// This Class sets all the variables for the terrain and AR-options.
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class GPS : MonoBehaviour
{
    public static GPS Instance { set; get; }

    #region Class Variables

    private GpsData currentGpsLocation;

    [SerializeField] public GpsData active;

    [Header("AR Settings")]
    private XROrigin xrOrigin;
    [SerializeField] private GameObject cameraOffset;
    [SerializeField] private Transform origin;
    [SerializeField] private Transform Camera;
    [SerializeField] private float cameraHeightOffset;
    [SerializeField] private Transform glacier;

    [Header("Terrain")]
    [SerializeField] private HeightModels heightModel;
    [SerializeField] private string openTopographyAPIKey;
    [SerializeField] private TerrainDataLoader terrainDataLoader;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] public double meshRangeInMeters = 11790;

    [Header("Terrain Generation")]
    [SerializeField] public Transform unityTerrainParent;
    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;

    private MeshFilter meshFilter;

    [Header("Glaciers")]
    [SerializeField] public Glacier[] glaciers;
    public Glacier activeGlacier;
    private GameObject glacierGameObject;

    [SerializeField] public bool simulateGpsLocation;
    [SerializeField] public GpsData simulatedGpsLocation;

    [Header("UI")]
    [SerializeField] public LoadingManager loadingManager;
    [SerializeField] public SceneSelector sceneSelector;


    [Header("Debug")]
    [SerializeField] public bool simulateEditorLocation = false;
    [SerializeField] public GpsData simulatedEditorLocation;


    [NonSerialized] public bool started = false;

    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        xrOrigin = origin.GetComponent<XROrigin>();
    }

    private void Update()
    {
        if (started)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    float rotationAmount = touch.deltaPosition.x * -0.1f;
                    cameraOffset.transform.Rotate(0, rotationAmount, 0);
                }
            }
        }


    }

    #endregion

    #region Class Methods
    /// <summary>
    /// Main Programm. Starts the Compass and the GPS. Gets the TerrainData from TerrainDataLoader and converts it into a Terrain, and places it accordingly.
    /// </summary>
    public void StartLoadingTerrain(bool simulateGPS)
    {
        simulateGpsLocation = simulateGPS;

        StartCoroutine(GetGPSPosition(() => {

            //ONLY IN UNITY EDITOR
            if (simulateEditorLocation)
            {
                currentGpsLocation = simulatedEditorLocation;
            }


            bool foundActiveGlacier = false;

            if (!simulateGpsLocation)
            {
                // Check if any glacier is in reach

                foreach (Glacier glacier in glaciers)
                {
                    bool isWithinLat = currentGpsLocation.lat >= glacier.south && currentGpsLocation.lat <= glacier.north;
                    bool isWithinLon = currentGpsLocation.lon >= glacier.west && currentGpsLocation.lon <= glacier.east;

                    Debug.Log(isWithinLat + " " + isWithinLon);

                    if (isWithinLat && isWithinLon)
                    {
                        activeGlacier = glacier;
                        foundActiveGlacier = true;
                        break;
                    }
                }
            } else
            {
                foundActiveGlacier = true;
                activeGlacier = glaciers[0];
            }

            if (foundActiveGlacier)
            {
                StartCoroutine(AdjustHeading());

                StartCoroutine(GetTerrainData(TerrainCreation));

                //Instantiate Glacier
                glacierGameObject = Instantiate(activeGlacier.fbxModel, glacier);
                // Get all Transforms
                glacierGameObject.transform.position = new Vector3(activeGlacier.position.x, activeGlacier.position.y, activeGlacier.position.z);
                glacierGameObject.transform.localScale = new Vector3(activeGlacier.scaling.x, activeGlacier.scaling.y, activeGlacier.scaling.z);
                glacierGameObject.transform.rotation = Quaternion.Euler(activeGlacier.rotation.x, activeGlacier.rotation.y, activeGlacier.rotation.z);

            }
            else
            {
                throw new Exception("No active glacier found within the specified GPS coordinates.");
                // Give feedback to the user to choose which glacier to simulate
            }
        }));
    }

    private IEnumerator GetGPSPosition(Action onComplete)
    {
        if (!simulateGpsLocation)
        {
            //GPS READING
            // Check if the user has location service enabled.
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location not enabled on device or app does not have permission to access location");
            }

            // Starts the location service.
            Input.location.Start();

            loadingManager.SetText("Start GPS");
            loadingManager.SetHeadingProgress(30);

            // Waits until the location service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // If the service didn't initialize in 20 seconds this cancels location service use.
            if (maxWait < 1)
            {
                Debug.Log("Timed out");
                yield break;
            }

            // If the connection failed this cancels location service use.
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("Unable to determine device location");
                yield break;
            }

            currentGpsLocation = new GpsData(Input.location.lastData.latitude, Input.location.lastData.longitude, Input.location.lastData.altitude);
        } 
        else {
            currentGpsLocation = simulatedGpsLocation;
        }

        loadingManager.SetText("Positioning done");
        loadingManager.SetHeadingProgress(100);

        onComplete?.Invoke();
    }

    public IEnumerator AdjustHeading()
    {
        // Enable the compass
        Input.compass.enabled = true;

        loadingManager.SetText("Reading Compass");
        loadingManager.SetHeadingProgress(30);

        // Wait a bit for the compass to start
        yield return new WaitForSeconds(1f);

        float heading = Input.compass.magneticHeading;

        //rotate Origin
        xrOrigin.MatchOriginUpCameraForward(Vector3.up, CoordinateConverter.HeadingToForwardVector(heading));

        started = true;

        loadingManager.SetText("Adjust Heading");
        loadingManager.SetHeadingProgress(100);
    }

    private void TerrainCreation(string[] result)
    {
        StartCoroutine(TerrainDataLoader.GetHeightsFromAsciiGrid(result, heightModel, OnHeightDataReady));
    }

    private void OnHeightDataReady(AsciiHeightData heightData)
    {
        StartCoroutine(TerrainDataLoader.CreateTerrainDataFromAsciiGridCoroutine(heightData, OnTerrainDataReady));
    }

    void OnTerrainDataReady(TerrainData terrainData)
    {
        terrain.terrainData = terrainData;

        // Set "resolution"
        terrain.heightmapPixelError = 20;

        terrain.materialTemplate = terrainMaterial;

        // assign the data to the collider
        terrainCollider.terrainData = terrain.terrainData;

        float terrainSizeInMeters = terrain.terrainData.size.x;

        // center the terrain
        terrain.transform.position = new Vector3(
            -terrainSizeInMeters / 2,
            terrain.transform.position.y,
            -terrainSizeInMeters / 2);

        // set height
        Vector3 terrainHeight = CalculatePositionOnTerrain(origin, cameraHeightOffset);
        unityTerrainParent.position = new Vector3(unityTerrainParent.position.x, -terrainHeight.y, unityTerrainParent.position.z);

        Debug.Log("Terrain setup complete");

        //EQUIRECTANGULAR
        /*Vector2 glacierUnityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(active, currentGpsLocation);
        glacier.position = new Vector3((float)(glacierUnityLocation.x), glacier.position.y, glacierUnityLocation.y);

        glacier.position = CalculatePositionOnTerrain(glacier);
        glacier.position = new Vector3(glacier.position.x, glacier.position.y + unityTerrainParent.position.y, glacier.position.z);
        */

        // Set Player Position on the terrain
        /*Vector2 playerPosition = CoordinateConverter.calculateRelativePositionEquirectangular2D(currentGpsLocation, activeGlacier.centerPosition);
        origin.position = new Vector3((float)(playerPosition.x), xrOrigin.transform.position.y, playerPosition.y);

        origin.position = CalculatePositionOnTerrain(origin);
        origin.position = new Vector3(origin.position.x, origin.position.y + unityTerrainParent.position.y, origin.position.z);
        */
        sceneSelector.LoadingDoneUI();
    }


    /// <summary>
    /// Calculates the Position on the Terrain.
    /// </summary>
    public Vector3 CalculatePositionOnTerrain(Transform t, float heightOffset = 0f)
    {
        float terrainHeight = terrain.SampleHeight(t.position);

        Vector3 positionOnTerrain = new Vector3(t.position.x, terrainHeight + heightOffset, t.position.z);

        Debug.Log("Terrain Position: " + t.name + " " + positionOnTerrain);

        return positionOnTerrain;
    }

    #endregion

    #region ASYNC Methods
    /// <summary>
    /// Gets the TerrainData from TerrainDataLoader.
    /// </summary>
    public IEnumerator GetTerrainData(Action<string[]> callback)
    {
        //choose which glacier we are at and download accordingly

        // Location: Aletsch Gletscher
        // Center: Dreieckhorn
        // Lat: 46.4779410
        // Lon: 8.0201572
        // Elevation: 3811m
        // Distance approx. 33'000 / 2 = 16'500m
        /*
        meshRangeInMeters = 33000;
        currentGpsLocation = new GpsData(46.4779410, 8.0201572, 0.0);
        

        double range = meshRangeInMeters / 2;

        double latDegreeDistance = range / 111000.0; // Convert range to latitude degrees
        double lonDegreeDistance = range / (Math.Cos(currentGpsLocation.lat * Math.PI / 180) * 111000.0); // Convert range to longitude degrees
        */
        yield return TerrainDataLoader.GetTerrainData(
            /*
        currentGpsLocation.lat + latDegreeDistance,
        currentGpsLocation.lat - latDegreeDistance,
        currentGpsLocation.lon - lonDegreeDistance,
        currentGpsLocation.lon + lonDegreeDistance,*/
        activeGlacier.north,
        activeGlacier.south,
        activeGlacier.west,
        activeGlacier.east,
        openTopographyAPIKey, heightModel,
        (data) => {
            // This is the callback that will be called once the data is fetched
            string[] fileArray = data.Split(new char[] { '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("TerrainData from API: " + data);
            callback(fileArray); // Call the callback with the fetched data
        });
    }

    #endregion
}