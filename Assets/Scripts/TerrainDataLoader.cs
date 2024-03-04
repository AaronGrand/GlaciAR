using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Jobs;
using Unity.Collections;
using System.Threading;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Loads Terrain Data from OpenTopography.org.
/// </summary>
public class TerrainDataLoader
{
    private static int YIELD_TIME = 10;

    public static int MAX_VERTICES_PER_MESH = 65535;
    public static int EarthRadius = 6371000;

    private void Start()
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
    }

    
    private const string BASE_URL = "https://portal.opentopography.org/API/globaldem";
    /// <summary>
    /// Gets a GeoTiff file in string format from GPS-boundaries from OpenTopography.org API.
    /// </summary>
    public static IEnumerator GetTerrainData(double gpsNorth, double gpsSouth, double gpsWest, double gpsEast, string apiKey, HeightModels heightModel, Action<string> callback)
    {
        string heightM = HeightModelUtility.GetAPIReference(heightModel);

        string fullUrl = $"{BASE_URL}?demtype=" + heightM + "&south=" + gpsSouth + "&north=" + gpsNorth + "&west=" + gpsWest + "&east=" + gpsEast + "&outputFormat=AAIGrid&API_Key=" + apiKey;
        Debug.Log(fullUrl);

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            www.SendWebRequest();

            while (!www.isDone)
            {
                GPS.Instance.loadingManager.SetText("Downloading Terrain");
                // 99 prevents error
                GPS.Instance.loadingManager.SetDownloadProgress((int)(www.downloadProgress * 99));

                yield return null;
            }

            // Check if the request completed successfully
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
                callback(null);
            }
            else
            {
                GPS.Instance.loadingManager.SetText("Terrain downloaded");
                GPS.Instance.loadingManager.SetDownloadProgress(100);
                // Handle successful download
                callback(www.downloadHandler.text);
            }
        }
    }


    /// <summary>
    /// Creates AsciiHeightData from a given AsciiGrid[] and a heightModel.
    /// </summary>
    public static IEnumerator GetHeightsFromAsciiGrid(string[] asciiLines, HeightModels heightModel, Action<AsciiHeightData> onCompleted)
    {
        Debug.Log("Getting Heights from AsciiGrid");
        int gridSizeInMeter = HeightModelUtility.GetGridSize(heightModel);

        // Parse metadata
        int ncols = int.Parse(asciiLines[1]);
        int nrows = int.Parse(asciiLines[3]);

        //double xllcorner = double.Parse(asciiLines[5]);
        //double yllcorner = double.Parse(asciiLines[7]);

        //double cellsize = double.Parse(asciiLines[9]);
        //double nodata_value = double.Parse(asciiLines[11]);

        // Extracting the height data
        float[,] heights = new float[nrows, ncols];
        for (int row = 0; row < nrows; row++)
        {
            for (int col = 0; col < ncols; col++)
            {
                heights[row, col] = float.Parse(asciiLines[12 + row * ncols + col]);
            }
            /*
            // Yield every few rows to spread the work over multiple frames
            if (row % YIELD_TIME == 0)
            {
                yield return null;
            }*/
        }
        
        //fixing aspect ratio
        int desiredAspectRatio = 1;
        double currentAspectRatio = (double)ncols / nrows;
        double scalingFactor = desiredAspectRatio / currentAspectRatio;

        Debug.Log("Scaling Factor: " + scalingFactor);

        AsciiHeightData data = new AsciiHeightData
        {
            heights = heights,
            colScalingFactor = scalingFactor,
            gridSizeInMeter = gridSizeInMeter,
            nrows = nrows,
            ncols = ncols
        };

        yield return null;

        onCompleted?.Invoke(data);
    }

    /// <summary>
    /// Creates TerrainData from a given AsciiHeightData.
    /// </summary>
    public static IEnumerator CreateTerrainDataFromAsciiGridCoroutine(AsciiHeightData data, Action<TerrainData> callback)
    {
        GPS.Instance.loadingManager.SetText("TerrainData");
        GPS.Instance.loadingManager.SetTerrainDataProgress(0);

        Debug.Log("Creating Terraindata");
        TerrainData terrainData = new TerrainData();
        // max Heightmap Resolution
        terrainData.heightmapResolution = 4097;

        // resample the heightmap, so it stretches the map to the Resolution
        float[,] heights = ResampleHeightmap(data.heights, terrainData.heightmapResolution);

        int originalRows = heights.GetLength(0); // Number of rows in the resampled heightmap
        int originalCols = heights.GetLength(1); // Number of columns in the resampled heightmap

        int mRow = data.nrows * data.gridSizeInMeter;



        Debug.Log("Find min/max Height");

        GPS.Instance.loadingManager.SetText("TerrainData min/max height");
        GPS.Instance.loadingManager.SetTerrainDataProgress(20);

        //find max height
        float maxHeight = 0;
        float minHeight = heights[0, 0];
        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                if (heights[i, j] > maxHeight)
                {
                    maxHeight = heights[i, j];
                }

                if (heights[i, j] < minHeight)
                {
                    minHeight = heights[i, j];
                }
            }
        }



        Debug.Log("Rotate TerrainData");

        GPS.Instance.loadingManager.SetText("TerrainData rotate");
        GPS.Instance.loadingManager.SetTerrainDataProgress(30);

        //set terrain size
        terrainData.size = new Vector3(mRow, (maxHeight - minHeight), mRow);

        // Adjusting dimensions for rotated matrix
        int newRows = originalCols;
        int newCols = originalRows;
        float[,] fHeights = new float[newRows, newCols];

        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                // Rotating 90 degrees counter-clockwise
                fHeights[newRows - 1 - j, i] = (heights[i, j] - minHeight) / (maxHeight - minHeight);   
            }
        }



        Debug.Log("Set Height on TerrainData");

        GPS.Instance.loadingManager.SetText("Creating TerrainData");
        GPS.Instance.loadingManager.SetTerrainDataProgress(50);

        yield return TerrainDataLoader.SetHeightsCoroutine(terrainData, fHeights);
        Debug.Log("TerrainDataLoader done");
        
        GPS.Instance.loadingManager.SetText("TerrainData created");
        GPS.Instance.loadingManager.SetTerrainDataProgress(100);

        // Use a callback to return the terrain data once processing is complete
        callback(terrainData);
    }

    private static IEnumerator SetHeightsCoroutine(TerrainData terrainData, float[,] fHeights)
    {
        int width = fHeights.GetLength(0);
        int height = fHeights.GetLength(1);

        const int chunkSize = 1000; // Size of the chunk to update at once, adjust based on performance
        for (int y = 0; y < height; y += chunkSize)
        {
            for (int x = 0; x < width; x += chunkSize)
            {
                int chunkWidth = Mathf.Min(chunkSize, width - x);
                int chunkHeight = Mathf.Min(chunkSize, height - y);

                float[,] chunkHeights = new float[chunkHeight, chunkWidth];
                for (int i = 0; i < chunkHeight; i++)
                {
                    for (int j = 0; j < chunkWidth; j++)
                    {
                        chunkHeights[i, j] = fHeights[y + i, x + j];
                    }
                }

                terrainData.SetHeightsDelayLOD(x, y, chunkHeights);
            }

            GPS.Instance.loadingManager.SetTerrainDataProgress((int)((float)50 / height) * 100 * y);
            yield return null;
        }

        yield return null;

        // After all chunks have been processed, apply all changes
        terrainData.SyncHeightmap();
    }

    /// <summary>
    /// Resamples a two-dimensional height-array to a new resolution using bilinear interpolation.
    /// </summary>
    private static float[,] ResampleHeightmap(float[,] originalHeights, int newResolution)
    {
        int originalWidth = originalHeights.GetLength(1);
        int originalHeight = originalHeights.GetLength(0);
        float[,] resampledHeights = new float[newResolution, newResolution];
        
        for (int i = 0; i < newResolution; i++)
        {
            for (int j = 0; j < newResolution; j++)
            {
                float xRatio = i / (float)(newResolution - 1);
                float yRatio = j / (float)(newResolution - 1);

                float x = xRatio * (originalWidth - 1);
                float y = yRatio * (originalHeight - 1);

                int xFloor = (int)x;
                int yFloor = (int)y;
                int xCeil = xFloor == originalWidth - 1 ? xFloor : xFloor + 1;
                int yCeil = yFloor == originalHeight - 1 ? yFloor : yFloor + 1;

                // Interpolation weights
                float xWeight = x - xFloor;
                float yWeight = y - yFloor;

                // Bilinear interpolation
                float topLeft = originalHeights[yFloor, xFloor];
                float topRight = originalHeights[yFloor, xCeil];
                float bottomLeft = originalHeights[yCeil, xFloor];
                float bottomRight = originalHeights[yCeil, xCeil];

                float top = topLeft * (1 - xWeight) + topRight * xWeight;
                float bottom = bottomLeft * (1 - xWeight) + bottomRight * xWeight;

                resampledHeights[i, j] = top * (1 - yWeight) + bottom * yWeight;
            }
        }
        
        return resampledHeights;
    }

}


/// <summary>
/// Contains all information for the HeightData
/// </summary>
public struct AsciiHeightData
{
    public float[,] heights;
    public double colScalingFactor;
    public int gridSizeInMeter;

    public int ncols;
    public int nrows;
}
