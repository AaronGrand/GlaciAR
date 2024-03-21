using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

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

    //[SerializeField] public GpsData active;

    [Header("AR Settings")]
    private XROrigin xrOrigin;
    [SerializeField] private GameObject cameraOffset;
    [SerializeField] private Transform origin;
    [SerializeField] private Transform Camera;
    [SerializeField] private float cameraHeightOffset;
    [SerializeField] private Transform glacier;
    
    [SerializeField] private Transform world;

    [Header("Terrain")]
    [SerializeField] private Material terrainMaterial;

    // Use for Terrain Generation after mesh download
    [Header("Terrain Generation")]
    [SerializeField] public Transform unityTerrainParent;
    [SerializeField] private Terrain terrain;
    //[SerializeField] private TerrainCollider terrainCollider;

    //private MeshFilter meshFilter;

    [Header("Glaciers")]
    [SerializeField] public Glacier[] glaciers;
    public Glacier activeGlacier;
    private GameObject glacierGameObject;
    //[SerializeField] private MeshFilter glacierbed;

    [SerializeField] private Material mat_seeThrough;
    [SerializeField] private Material mat_simulation;

    private GlacierObject glacierObject;

    [Header("UI")]
    [SerializeField] public LoadingManager loadingManager;
    [SerializeField] public SceneSelector sceneSelector;


    [Header("Debug")]
    [SerializeField] public bool editorSimulateLocation = false;
    [SerializeField] public GpsData editorSimulatedLocation;


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
        /*
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
        }*/
    }

    #endregion

    #region Class Methods
    /// <summary>
    /// After Deciding what Glacier to display, we need to set the GPS Position. In simulation, but also in real GPS Reading.
    /// </summary>
    public void StartLoadingTerrain(bool simulateGPS)
    {
        bool hasException = false;

        try
        {
            // Reset parameters
            world.position = Vector3.zero;
            glacier.position = Vector3.zero;

            // Simulated GPS Position (Not on site)
            if (simulateGPS)
            {
                currentGpsLocation = activeGlacier.centerPosition;

                // TODO: Set player position for simulation


                // Get GPS Position
            }
            else
            {
                // Reset parameters
                bool foundActiveGlacier = false;

                // Get GPS Position
                {
                    StartCoroutine(AdjustHeading());

                    StartCoroutine(GetGPSPosition(() => {
                        try
                        {
                            // Check if glacier is in reach
                            bool glacierIsWithinLat = currentGpsLocation.lat >= activeGlacier.south && currentGpsLocation.lat <= activeGlacier.north;
                            bool glacierIsWithinLon = currentGpsLocation.lon >= activeGlacier.west && currentGpsLocation.lon <= activeGlacier.east;

                            foundActiveGlacier = (glacierIsWithinLat && glacierIsWithinLon);

                            // Not in Reach of the Glacier
                            if (!foundActiveGlacier)
                            {
                                throw new Exception("Not in reach of selected glacier. Please select simulation.");
                            }
                            else
                            {
                                // Set Player Position on the terrain
                                Vector2 playerPosition = CoordinateConverter.calculateRelativePositionEquirectangular2D(activeGlacier.centerPosition, currentGpsLocation);
                                world.position = new Vector3((float)(playerPosition.x), world.position.y, playerPosition.y);

                                world.position = CalculatePositionOnTerrain(world);
                                world.position = new Vector3(world.position.x, world.position.y - unityTerrainParent.position.y - CalculatePositionOnTerrain(xrOrigin.transform).y - cameraHeightOffset, world.position.z);
                            }
                        } catch (Exception e)
                        {
                            hasException = true;
                            LoadingException(e);
                        }
                    }));
                }
            }

            if (!hasException)
            {
                // Instantiate Glacier
                glacierGameObject = Instantiate(activeGlacier.glacier, glacier);
                // Get all Transforms
                //glacierGameObject.transform.position = new Vector3(activeGlacier.position.x, activeGlacier.position.y, activeGlacier.position.z);
                //glacierGameObject.transform.localScale = new Vector3(activeGlacier.scaling.x, activeGlacier.scaling.y, activeGlacier.scaling.z);
                //glacierGameObject.transform.rotation = Quaternion.Euler(activeGlacier.rotation.x, activeGlacier.rotation.y, activeGlacier.rotation.z);

                glacierObject = glacierGameObject.GetComponent<GlacierObject>();
                if (glacierObject)
                {
                    sceneSelector.glaciARSlider.minValue = 0;
                    // reset
                    sceneSelector.glaciARSlider.value = 0;
                    glacierObject.SetGlacier(0);

                    sceneSelector.glaciARSlider.maxValue = glacierObject.glacierStates.Length - 1;
                    sceneSelector.glaciARSlider.onValueChanged.AddListener((value) => {
                        // Set text of Slider and change glacier state
                        glacierObject.SetGlacier(Mathf.RoundToInt(value));
                    });

                    // Set Material to simulationMaterial
                    if (simulateGPS)
                    {
                        glacierObject.terrain.GetComponent<Renderer>().material = mat_simulation;
                        // Set Material to seeThrough
                    } else
                    {
                        glacierObject.terrain.GetComponent<Renderer>().material = mat_seeThrough;
                    }

                }
                else
                {
                    hasException = true;

                    throw new Exception("No glacierObject found.");
                    // Set back to Menu
                }
            }

            if (!hasException)
            {
                sceneSelector.LoadingDoneUI();
            }

        } catch (Exception e)
        {
            LoadingException(e);
        }
    }

    public void ResetGlacier()
    {
        Destroy(glacierGameObject);
    }

    private void LoadingException(Exception e)
    {
        Console.WriteLine($"Caught an exception: {e.Message}");
        sceneSelector.OnMenuClicked();
    }

    private IEnumerator GetGPSPosition(Action onComplete)
    {
        // Editor GPS Position
        if (editorSimulateLocation)
        {
            currentGpsLocation = editorSimulatedLocation;
            // GPS Reading
        }
        else
        {
            // Check if the user has location service enabled.
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location not enabled on device or app does not have permission to access location");
            }

            // Starts the location service.
            Input.location.Start();

            //loadingManager.SetText("Start GPS");
            //loadingManager.SetHeadingProgress(30);

            // Waits until the location service initializes
            int maxWait = 10000;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
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

        //loadingManager.SetText("Positioning done");
        //loadingManager.SetHeadingProgress(100);
        onComplete?.Invoke();
    }

    public IEnumerator AdjustHeading()
    {
        // Enable the compass
        Input.compass.enabled = true;

        //loadingManager.SetText("Reading Compass");
        //loadingManager.SetHeadingProgress(30);

        // Wait a bit for the compass to start
        yield return new WaitForSeconds(1.5f);

        float heading = Input.compass.magneticHeading;

        //rotate Origin
        xrOrigin.MatchOriginUpCameraForward(Vector3.up, CoordinateConverter.HeadingToForwardVector(heading));

        started = true;

        //loadingManager.SetText("Adjust Heading");
        //loadingManager.SetHeadingProgress(100);
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
    
    #endregion
}