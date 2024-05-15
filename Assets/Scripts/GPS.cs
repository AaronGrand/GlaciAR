using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages AR-based GPS location tracking and visualization of glaciers.
/// Provides methods for initializing and managing camera settings, terrain interaction,
/// and dynamic content loading related to glacier visualization using augmented reality (AR).
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class GPS : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the GPS class.
    /// </summary>
    public static GPS Instance { private set; get; }

    [Header("AR Settings")]
    [SerializeField] private Transform origin;
    [SerializeField] public GameObject cameraOffset;
    [SerializeField] private float cameraHeightOffset;
    [SerializeField] private Transform world;
    [SerializeField] private Transform glacier;
    [SerializeField] private Transform pointOfInterestObject;
    [SerializeField] private Camera arCamera;
    private XROrigin xrOrigin;

    [Header("Simulation Settings")]
    [SerializeField] private bool editorSimulateLocation = false;
    [SerializeField] private GpsData editorSimulatedLocation;
    [SerializeField] private Vector3 simulatePosition;

    [Header("Glaciers")]
    public Glacier[] glaciers;
    public Glacier activeGlacier;
    private GameObject glacierGameObject;
    private GlacierObject glacierObject;

    [SerializeField] private string pointOfInterestJSON;

    [Header("UI")]
    public LoadingManager loadingManager;
    public SceneSelector sceneSelector;
    public event Action onLoadingComplete;
    public ExceptionHandler exceptionHandler;

    [Header("Terrain")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Material mat_seeThrough;

    [Header("Debug")]
    public bool started = false;

    private GpsData currentGpsLocation;
    public bool outline = true;

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Starts necessary services and requests permissions for using camera and GPS.
    /// </summary>
    private void Start()
    {
        xrOrigin = GetComponent<XROrigin>();
        StartCoroutine(RequestCameraPermission());
    }

    /// <summary>
    /// Updates the state each frame, handling camera panning if necessary.
    /// </summary>
    private void Update()
    {
        if (started)
        {
            HandleCameraPanning();
        }
    }

    /// <summary>
    /// Handles horizontal camera panning based on user touch input.
    /// </summary>
    private void HandleCameraPanning()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float rotationAmount = touch.deltaPosition.x * -0.05f;
                cameraOffset.transform.Rotate(0, rotationAmount, 0);
            }
        }
    }

    /// <summary>
    /// Changes the application language and loads corresponding text assets.
    /// </summary>
    /// <param name="language">The language to load.</param>
    public void SetLanguage(string language)
    {
        StartCoroutine(LanguageTextManager.LoadLocalizedText(language));
    }

    /// <summary>
    /// Sets the active glacier to visualize based on the provided index.
    /// </summary>
    /// <param name="index">Index of the glacier in the array.</param>
    public void SetGlacier(int index)
    {
        if (index >= glaciers.Length)
        {
            exceptionHandler.ShowErrorMessage($"Error: \nNo glacier with index {index} found!\nTry delete cache and load again.");
            ResetGlacier();
            throw new IndexOutOfRangeException($"No glacier with index {index} found!");
        }
        activeGlacier = glaciers[index];
    }

    /// <summary>
    /// Requests necessary permissions for using the camera and location services.
    /// </summary>
    public IEnumerator RequestCameraPermission()
    {
#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            exceptionHandler.ShowErrorMessage("Camera access is needed for AR features. Please grant permissions to continue.");
            yield return new WaitForSeconds(1);
        }
#endif

    }
    public IEnumerator RequestLocationPermission()
    {
#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) || !Input.location.isEnabledByUser)
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            exceptionHandler.ShowErrorMessage("Location access is needed to load terrain and glacier data.");
            yield return new WaitForSeconds(1);
        }
#endif
        LoadTerrain();
    }

    /// <summary>
    /// Begins the loading of terrain data, optionally simulating GPS data.
    /// </summary>
    /// <param name="simulateGPS">Whether to use simulated GPS data.</param>
    public void StartLoadingTerrain(bool simulateGPS)
    {
        loadingManager.SetText(LanguageTextManager.GetLocalizedValue("start_progress"));

        if (simulateGPS)
        {
            SimulateTerrainLoading();
        }
        else
        {
            // Requests Location Permission and starts the loading of terrain afterwards
            StartCoroutine(RequestLocationPermission());
        }
    }

    /// <summary>
    /// Simulates the loading of terrain and glacier data.
    /// </summary>
    private void SimulateTerrainLoading()
    {
        cameraOffset.transform.position = simulatePosition;
        LoadGlacier();
    }

    /// <summary>
    /// Initiates the process to fetch the user's GPS location and loads the terrain data accordingly.
    /// </summary>
    private void LoadTerrain()
    {
        StartCoroutine(GetGPSPosition(LoadGlacier));
    }

    /// <summary>
    /// Coroutine to acquire the GPS position of the device.
    /// </summary>
    /// <param name="onComplete">Callback to execute upon acquiring the position.</param>
    /// <returns>Returns an IEnumerator to manage the coroutine flow.</returns>
    private IEnumerator GetGPSPosition(Action onComplete)
    {
        loadingManager.SetText(LanguageTextManager.GetLocalizedValue("gps_progress_0"));
        loadingManager.SetGPSProgress(10);

        if (editorSimulateLocation)
        {
            currentGpsLocation = editorSimulatedLocation;
            onComplete();
            yield break;
        }

        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled on device or app does not have permission to access location");
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        Input.location.Start();

        int maxWait = 10000;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return null;
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            yield break;
        }

        currentGpsLocation = new GpsData(Input.location.lastData.latitude, Input.location.lastData.longitude, Input.location.lastData.altitude);

        loadingManager.SetGPSProgress(100);

        onComplete();
    }

    /// <summary>
    /// Instantiates the glacier object based on the current active glacier data.
    /// </summary>
    private void LoadGlacier()
    {
        loadingManager.SetText(LanguageTextManager.GetLocalizedValue("load_terrain_progress_0"));
        loadingManager.SetDownloadProgress(10);

        Addressables.InstantiateAsync(activeGlacier.glacier).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                HandleGlacierLoaded(handle.Result);

                StaticPointOfInterestManager.LoadLocalizedMountains(pointOfInterestJSON, pointOfInterestObject);

                loadingManager.SetDownloadProgress(100);
            }
            else
            {
                exceptionHandler.ShowErrorMessage("Error: Glacier could not be loaded.\nCheck Internet Connection.");
                ResetGlacier();
            }
        };
    }

    /// <summary>
    /// Resets the visualized glacier and clears any associated data.
    /// </summary>
    public void ResetGlacier()
    {
        if (glacierGameObject != null)
        {
            Destroy(glacierGameObject);
            glacierGameObject = null;
        }

        activeGlacier = null;
        if (glacierObject != null)
        {
            glacierObject.SetGlacier(0);
        }

        foreach (Transform child in pointOfInterestObject.transform)
        {
            Destroy(child.gameObject);
        }

        started = false;

        loadingManager.ResetProgress();
        sceneSelector.ResetUI();
    }




    /// <summary>
    /// Handles the completion of glacier object loading.
    /// </summary>
    /// <param name="glacierInstance">The instantiated glacier object.</param>
    private void HandleGlacierLoaded(GameObject glacierInstance)
    {
        glacierGameObject = glacierInstance;
        glacierGameObject.transform.SetParent(world);
        glacierGameObject.transform.position = world.position;

        glacierObject = glacierGameObject.GetComponent<GlacierObject>();
        if (glacierObject != null)
        {
            sceneSelector.SetupSlider(glacierObject.glacierStates.Length, (value) =>
            {
                glacierObject.SetGlacier(Mathf.RoundToInt(value));
            });

            started = true;
            onLoadingComplete?.Invoke();
        }
        else
        {
            exceptionHandler.ShowErrorMessage("Error: No glacier found.");
            ResetGlacier();

            Debug.LogError("No GlacierObject component found.");
        }
    }

    /// <summary>
    /// Toggles the visibility of terrain outlines for the active glacier.
    /// </summary>
    public void ToggleGlacierOutline()
    {
        if (glacierObject != null)
        {
            glacierObject.ToggleTerrainOutline();
        }
    }

    /// <summary>
    /// Toggles the visibility of points of interest.
    /// </summary>
    public void TogglePointOfInterest()
    {
        pointOfInterestObject.gameObject.SetActive(!pointOfInterestObject.gameObject.activeSelf);
    }

    /// <summary>
    /// Calculates the position on the terrain mesh, adjusted by a specified height offset.
    /// </summary>
    /// <param name="position">The position to check on the mesh.</param>
    /// <param name="heightOffset">Additional height to adjust the calculated position.</param>
    /// <returns>Returns the adjusted position on the terrain mesh.</returns>
    public Vector3 CalculatePositionOnMesh(Vector3 position, float heightOffset = 0f)
    {
        RaycastHit ray;
        Vector3 pos = new Vector3(position.x, position.y + 50000f, position.z);
        if (Physics.Raycast(pos, Vector3.down, out ray, 100000, terrainLayer))
        {
            Debug.Log("RAY DOWN HIT");
        }
        else
        {
            Physics.Raycast(pos, Vector3.up, out ray, 100000, terrainLayer);
            Debug.Log("RAY UP HIT");
        }
        return new Vector3(ray.point.x, ray.point.y + heightOffset, ray.point.z);
    }
}
