using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARCameraManager))]

public class CameraFeedToTexture : MonoBehaviour
{
    private ARCameraManager cameraManager;
    public Texture2D cameraTexture;

    void Awake()
    {
        cameraManager = GetComponent<ARCameraManager>();
    }

    void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        XRCpuImage image;
        if (cameraManager.TryAcquireLatestCpuImage(out image))
        {
            ConvertToTexture2D(image);
            image.Dispose();
        }
    }

    void ConvertToTexture2D(XRCpuImage image)
    {
        // Check if the texture exists and matches the size of the image
        if (cameraTexture == null || cameraTexture.width != image.width || cameraTexture.height != image.height)
        {
            if (cameraTexture != null)
            {
                Destroy(cameraTexture);
            }
            cameraTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        }

        // Conversion parameters
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        // Convert the image to the format required by the texture
        var rawTextureData = cameraTexture.GetRawTextureData<byte>();
        image.Convert(conversionParams, rawTextureData);

        cameraTexture.Apply();
    }
}
