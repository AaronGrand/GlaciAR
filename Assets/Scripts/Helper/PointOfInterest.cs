using UnityEngine;

/// <summary>
/// Controls the orientation of an object to ensure it always faces the camera.
/// This class is used to manage points of interest, making sure they are always oriented towards the user.
/// </summary>
public class PointOfInterest : MonoBehaviour
{
    private Camera mainCamera;

    /// <summary>
    /// Retrieves the main camera from the scene at the start.
    /// </summary>
    void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Updates the orientation of the point of interest every frame to face the camera.
    /// This ensures that the text or object at the point of interest remains readable from the camera's perspective.
    /// </summary>
    void Update()
    {
        // Rotate the text to face the camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, Vector3.up);
    }
}

