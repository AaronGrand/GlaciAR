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

/// <summary>
/// Loads Terrain Data from OpenTopography.org.
/// </summary>
public class TerrainDataLoader
{

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
    public static async Task<string> GetTerrainData(double gpsNorth, double gpsSouth, double gpsWest, double gpsEast, string apiKey, HeightModels heightModel)
    {
        string heightM = HeightModelUtility.GetAPIReference(heightModel);

        string fullUrl = $"{BASE_URL}?demtype=" + heightM + "&south=" + gpsSouth + "&north=" + gpsNorth + "&west=" + gpsWest + "&east=" + gpsEast + "&outputFormat=AAIGrid&API_Key=" + apiKey;
        Debug.Log(fullUrl);

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            UnityWebRequestAsyncOperation asyncOp = www.SendWebRequest();

            while (!asyncOp.isDone)
            {
                float progress = asyncOp.progress;
                ulong bytesDownloaded = www.downloadedBytes;
                long bytesTotal = www.GetResponseHeader("Content-Length") != null ? long.Parse(www.GetResponseHeader("Content-Length")) : -1;

                //if(bytesTotal != -1)
                {
                    Debug.Log($"Downloaded {bytesDownloaded} of {bytesTotal} bytes. {progress * 100}% completed.");
                }

                await Task.Delay(50); // Wait for a short duration before checking again
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
                return null;
            }
            else
            {
                return www.downloadHandler.text;
            }
        }
    }

    
    /// <summary>
    /// Creates AsciiHeightData from a given AsciiGrid[] and a heightModel.
    /// </summary>
    public static AsciiHeightData GetHeightsFromAsciiGrid(string[] asciiLines, HeightModels heightModel)
    {
        int gridSizeInMeter = HeightModelUtility.GetGridSize(heightModel);

        // Parse metadata
        int ncols = int.Parse(asciiLines[1]);
        int nrows = int.Parse(asciiLines[3]);

        double xllcorner = double.Parse(asciiLines[5]);
        double yllcorner = double.Parse(asciiLines[7]);

        double cellsize = double.Parse(asciiLines[9]);
        double nodata_value = double.Parse(asciiLines[11]);

        // Extracting the height data
        float[,] heights = new float[nrows, ncols];
        for (int row = 0; row < nrows; row++)
        {
            for (int col = 0; col < ncols; col++)
            {
                heights[row, col] = float.Parse(asciiLines[12 + row * ncols + col]);
            }
        }
        
        //fixing aspect ratio
        int desiredAspectRatio = 1;
        double currentAspectRatio = (double)ncols / nrows;
        double scalingFactor = desiredAspectRatio / currentAspectRatio;

        double rowOffsetToMiddle = nrows * gridSizeInMeter / 2;
        double colOffsetToMiddle = ncols * scalingFactor * gridSizeInMeter / 2;
        
        AsciiHeightData data;

        data.heights = heights;
        data.colScalingFactor = scalingFactor;
        data.gridSizeInMeter = gridSizeInMeter;

        data.nrows = nrows;
        data.ncols = ncols;

        return data;
    }

    /// <summary>
    /// Creates TerrainData from a given AsciiHeightData.
    /// </summary>
    public static TerrainData CreateTerrainDataFromAsciiGrid(AsciiHeightData data)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {

        }
        TerrainData terrainData = new TerrainData();
        // max Heightmap Resolution
        terrainData.heightmapResolution = 4097;

        // resample the heightmap, so it stretches the map to the Resolution
        float[,] heights = ResampleHeightmap(data.heights, terrainData.heightmapResolution);

        int originalRows = heights.GetLength(0); // Number of rows in the resampled heightmap
        int originalCols = heights.GetLength(1); // Number of columns in the resampled heightmap

        int mRow = data.nrows * data.gridSizeInMeter;

        //find max height
        int maxHeight = 0;
        int minHeight = (int)heights[0, 0];
        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                if (heights[i, j] > maxHeight)
                {
                    maxHeight = (int)heights[i, j];
                }

                if (heights[i, j] < minHeight)
                {
                    minHeight = (int)heights[i, j];
                }
            }
        }

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
                fHeights[newRows - 1 - j, i] = (heights[i, j] - minHeight) / (maxHeight - minHeight) + (minHeight / (maxHeight - minHeight));
            }
        }

        terrainData.SetHeights(0, 0, fHeights);

        return terrainData;
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
