using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// The Main Class of the Tool.
/// This Class sets all the variables for the terrain and AR-options.
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class GPS : MonoBehaviour
{

    // Glacier Indizes
    // Aletsch Gletscher:   00
    // ...

    public static GPS Instance { set; get; }

    #region Class Variables

    private GpsData currentGpsLocation;

    [Header("AR Settings")]
    private XROrigin xrOrigin;
    [SerializeField] private GameObject cameraOffset;
    [SerializeField] private Transform origin;
    [SerializeField] public Transform Camera;
    [SerializeField] private float cameraHeightOffset;
    [SerializeField] private Transform glacier;
    [SerializeField] private Transform pointOfInterestObject;
    
    [SerializeField] private Transform world;

    [SerializeField] private Vector3 simulatePosition;

    [Header("Terrain")]
    [SerializeField] private LayerMask terrainLayer;

    public bool outline = true;

    [Header("Glaciers")]
    [SerializeField] public Glacier[] glaciers;
    public Glacier activeGlacier;
    public GameObject glacierGameObject;

    [SerializeField] private Material mat_seeThrough;

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
        started = false;
        xrOrigin = origin.GetComponent<XROrigin>();
    }

    private void Update()
    {
        
        if (started)
        {
            Debug.Log("started true");
            if (Input.touchCount > 0)
            {
                Debug.Log("touch count > 0");
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    Debug.Log("Moved true");
                    float rotationAmount = touch.deltaPosition.x * -0.05f;
                    cameraOffset.transform.Rotate(0, rotationAmount, 0);
                }
            }
        }
    }

    #endregion

    #region Class Methods

    public void SetLanguage(string language)
    {
        StartCoroutine(LanguageTextManager.LoadLocalizedText(language));
    }

    public void SetGlacier(int index)
    {
        if (index >= glaciers.Length)
        {
            throw new IndexOutOfRangeException("No glacier with index " + index + " found!");
        }
        activeGlacier = glaciers[index];
    }

    public void RequestPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        if (!Input.location.isEnabledByUser)
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
    }

    /// <summary>
    /// After Deciding what Glacier to display, we need to set the GPS Position. In simulation, but also in real GPS Reading.
    /// </summary>
    public void StartLoadingTerrain(bool simulateGPS)
    {
        bool hasException = false;

        cameraOffset.transform.position = Vector3.zero;
        world.position = Vector3.zero;

        outline = true;
        pointOfInterestObject.gameObject.SetActive(true);

        started = false;

        try
        {

            // Simulated GPS Position (Not on site)
            if (simulateGPS)
            {
                currentGpsLocation = activeGlacier.centerPosition;

                // TODO: Set player position for simulation
                cameraOffset.transform.position = simulatePosition;

                // Get GPS Position
                {


                    loadingManager.SetGPSProgress(100);

                    loadingManager.SetTerrainDataProgress(0);
                    loadingManager.SetText(LanguageTextManager.GetLocalizedValue("load_terrain_progress_0"));

                    if (!hasException)
                    {
                        // Instantiate Glacier
                        Addressables.InstantiateAsync(activeGlacier.glacier).Completed += handle => {
                            if (handle.Status == AsyncOperationStatus.Succeeded)
                            {
                                // Asset is now instantiated.
                                glacierGameObject = handle.Result;

                                // Set parent
                                glacierGameObject.transform.parent = world;
                                glacierGameObject.transform.position = world.transform.position;

                                // Perform actions with glacierObject here

                                glacierObject = glacierGameObject.GetComponent<GlacierObject>();
                                if (glacierObject)
                                {
                                    // reset
                                    sceneSelector.glaciARSlider.minValue = 0;
                                    sceneSelector.glaciARSlider.value = 0;
                                    glacierObject.SetGlacier(0);

                                    sceneSelector.glaciARSlider.maxValue = glacierObject.glacierStates.Length - 1;
                                    sceneSelector.glaciARSlider.onValueChanged.AddListener((value) => {
                                        // Set text of Slider and change glacier state
                                        glacierObject.SetGlacier(Mathf.RoundToInt(value));
                                    });

                                    loadingManager.SetTerrainDataProgress(50);
                                    loadingManager.SetText(LanguageTextManager.GetLocalizedValue("textures_progress_0"));

                                    // Set Material to simulationMaterial
                                    if (simulateGPS)
                                    {
                                        glacierObject.terrain.GetComponent<Renderer>().material = activeGlacier.mat_terrain;
                                    }
                                    else
                                    {
                                        glacierObject.terrain.GetComponent<Renderer>().material = mat_seeThrough;
                                        Debug.Log("Set material");
                                    }

                                    glacierObject.glacierBed.GetComponent<Renderer>().material = activeGlacier.mat_glacierBed;
                                    glacierObject.SetMaterial(activeGlacier.mat_glacier);

                                }
                                else
                                {
                                    hasException = true;

                                    throw new Exception("No glacierObject found.");
                                    // Set back to Menu
                                }

                                // Set Points Of Interest
                                StaticPointOfInterestManager.LoadLocalizedMountains(activeGlacier.pointOfInterestFileName, pointOfInterestObject);

                            }
                            else
                            {
                                // Handle the case where instantiation failed
                                Debug.LogError("Asset instantiation failed.");
                                hasException = true;
                            }
                            if (!simulateGPS)
                            {
                                cameraOffset.transform.position = new Vector3(cameraOffset.transform.position.x, CalculatePositionOnMesh(cameraOffset.transform.position).y + cameraHeightOffset, cameraOffset.transform.position.z);
                            }

                            // At this point everything loaded
                            if (!hasException)
                            {
                                started = true;
                                sceneSelector.LoadingDoneUI();
                            }
                        };
                    }
                }
            }
            else
            {

                // Reset parameters
                bool foundActiveGlacier = false;

                // Get GPS Position
                {
                    loadingManager.SetHeadingProgress(0);
                    loadingManager.SetText(LanguageTextManager.GetLocalizedValue("heading_progress_0"));
                    StartCoroutine(AdjustHeading());

                    loadingManager.SetHeadingProgress(100);

                     
                    loadingManager.SetGPSProgress(0);
                    loadingManager.SetText(LanguageTextManager.GetLocalizedValue("gps_progress_0"));

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
                                Debug.Log(currentGpsLocation.lat + " " + currentGpsLocation.lon);
                                Vector2 glacierUnityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(activeGlacier.centerPosition, currentGpsLocation);
                                cameraOffset.transform.position = new Vector3((float)(glacierUnityLocation.x), world.position.y, (float)(glacierUnityLocation.y));
                            }



                            loadingManager.SetGPSProgress(100);

                            loadingManager.SetTerrainDataProgress(0);
                            loadingManager.SetText(LanguageTextManager.GetLocalizedValue("load_terrain_progress_0"));

                            if (!hasException)
                            {
                                // Instantiate Glacier
                                Addressables.InstantiateAsync(activeGlacier.glacier).Completed += handle => {
                                    if (handle.Status == AsyncOperationStatus.Succeeded)
                                    {
                                        // Asset is now instantiated.
                                        glacierGameObject = handle.Result;

                                        // Set parent
                                        glacierGameObject.transform.parent = world;
                                        glacierGameObject.transform.position = world.transform.position;

                                        // Perform actions with glacierObject here

                                        glacierObject = glacierGameObject.GetComponent<GlacierObject>();
                                        if (glacierObject)
                                        {
                                            // reset
                                            sceneSelector.glaciARSlider.minValue = 0;
                                            sceneSelector.glaciARSlider.value = 0;
                                            glacierObject.SetGlacier(0);

                                            sceneSelector.glaciARSlider.maxValue = glacierObject.glacierStates.Length - 1;
                                            sceneSelector.glaciARSlider.onValueChanged.AddListener((value) => {
                                                // Set text of Slider and change glacier state
                                                glacierObject.SetGlacier(Mathf.RoundToInt(value));
                                            });

                                            loadingManager.SetTerrainDataProgress(50);
                                            loadingManager.SetText(LanguageTextManager.GetLocalizedValue("textures_progress_0"));

                                            // Set Material to simulationMaterial
                                            if (simulateGPS)
                                            {
                                                glacierObject.terrain.GetComponent<Renderer>().material = activeGlacier.mat_terrain;
                                            }
                                            else
                                            {
                                                glacierObject.terrain.GetComponent<Renderer>().material = mat_seeThrough;
                                                Debug.Log("Set material");
                                            }

                                            glacierObject.glacierBed.GetComponent<Renderer>().material = activeGlacier.mat_glacierBed;
                                            glacierObject.SetMaterial(activeGlacier.mat_glacier);

                                        }
                                        else
                                        {
                                            hasException = true;

                                            throw new Exception("No glacierObject found.");
                                            // Set back to Menu
                                        }

                                        // Set Points Of Interest
                                        StaticPointOfInterestManager.LoadLocalizedMountains(activeGlacier.pointOfInterestFileName, pointOfInterestObject);

                                    }
                                    else
                                    {
                                        // Handle the case where instantiation failed
                                        Debug.LogError("Asset instantiation failed.");
                                        hasException = true;
                                    }
                                    if (!simulateGPS)
                                    {
                                        cameraOffset.transform.position = new Vector3(cameraOffset.transform.position.x, CalculatePositionOnMesh(cameraOffset.transform.position).y + cameraHeightOffset, cameraOffset.transform.position.z);
                                    }

                                    // At this point everything loaded
                                    if (!hasException)
                                    {
                                        started = true;
                                        sceneSelector.LoadingDoneUI();
                                    }
                                };
                            }

                        } catch (Exception e)
                        {
                            hasException = true;
                            LoadingException(e);
                        }
                    }));
                }
            }

        } catch (Exception e)
        {
            LoadingException(e);
        }
    }

    public void ResetGlacier()
    {
        Destroy(glacierGameObject);

        for (int i = pointOfInterestObject.childCount - 1; i >= 0; i--)
        {
            Destroy(pointOfInterestObject.GetChild(i).gameObject);
        }
    }

    public void ToggleGlacierOutline()
    {
        glacierObject.ToggleTerrainOutline();
    }

    public void TogglePointOfInterest()
    {
        pointOfInterestObject.gameObject.SetActive(!pointOfInterestObject.gameObject.activeSelf);
    }

    private void LoadingException(Exception e)
    {
        Console.WriteLine($"Caught an exception: {e.Message}");
        ResetGlacier();
        RequestPermissions();
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
                Permission.RequestUserPermission(Permission.FineLocation);
            }

            // Starts the location service.
            Input.location.Start();

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

        //loadingManager.SetText("Adjust Heading");
        //loadingManager.SetHeadingProgress(100);
    }

    /// <summary>
    /// Calculates the Position on the Terrain.
    /// </summary>
    /*public Vector3 CalculatePositionOnTerrain(Transform t, float heightOffset = 0f)
    {
        float terrainHeight = terrain.SampleHeight(t.position);

        Vector3 positionOnTerrain = new Vector3(t.position.x, terrainHeight + heightOffset, t.position.z);

        Debug.Log("Terrain Position: " + t.name + " " + positionOnTerrain);

        return positionOnTerrain;
    }*/

    /// <summary>
    /// Calculates the Position on a Mesh (with MeshCollider).
    /// </summary>
    public Vector3 CalculatePositionOnMesh(Vector3 position, float heightOffset = 0f)
    {
        Vector3 pos = new Vector3(position.x, position.y + 50000f, position.z);

        int layer = terrainLayer;
        RaycastHit ray;
        if (Physics.Raycast(pos, Vector3.down, out ray, 100000, layer))
        {
            Debug.Log("RAY DOWN HIT");
        }
        else
        {
            Physics.Raycast(pos, Vector3.up, out ray, 100000, layer);
            Debug.Log("RAY UP HIT");
        }

        Vector3 temp = ray.point;
        Debug.Log("Temp: " + temp.ToString());
        return new Vector3(temp.x, temp.y + heightOffset, temp.z);
    }

    #endregion

    #region ASYNC Methods

    #endregion
}