using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Schema;

public class TerrainHelperEditorWindow : EditorWindow
{
    enum EHeightMapImportMode
    {
        FitImageToTerrainHeightMap,
        ResizeTerrainHeightMapToMatchImage
    }

    Terrain SelectedTerrain;

    [MenuItem("Tools/Terrain Helper")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow<TerrainHelperEditorWindow>();
        window.titleContent = new GUIContent("Terrain Helper");

        window.Reset();
    }

    private void OnDestroy()
    {
        Reset();
    }

    private void Reset()
    {
        HeightMapTool_ImportExpanded = true;
        HeightMapTool_SelectedImageFilePath = string.Empty;
        HeightMapTool_SelectedImagePathValid = false;
        HeightMapTool_SelectedImage = null;
        HeightMapTool_ImportMode = EHeightMapImportMode.FitImageToTerrainHeightMap;
        HeightMapTool_Intensity = 1f;

        HeightMapTool_ExportExpanded = true;
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label("Not available while playing");
            return;
        }

        // draw the terrain selector - early out if nothing selected
        SelectedTerrain = (Terrain) EditorGUILayout.ObjectField("Selected Terrain", SelectedTerrain, typeof(Terrain), true);
        if (SelectedTerrain == null)
            return;

        OnGUI_HeightMapTools();
    }

    static int[] ValidHeightMapResolutions = new int[] { 33, 65, 129, 257, 513, 1025, 2049, 4097 };

    bool HeightMapTool_ImportExpanded = true;
    string HeightMapTool_SelectedImageFilePath;
    bool HeightMapTool_SelectedImagePathValid = false;
    Texture2D HeightMapTool_SelectedImage;
    EHeightMapImportMode HeightMapTool_ImportMode = EHeightMapImportMode.FitImageToTerrainHeightMap;
    float HeightMapTool_Intensity = 1f;

    bool HeightMapTool_ExportExpanded = true;

    void OnGUI_HeightMapTools()
    {
        EditorGUILayout.LabelField("Height Map Tools", EditorStyles.boldLabel);

        HeightMapTool_ImportExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(HeightMapTool_ImportExpanded, "Import Height Map");

        if (HeightMapTool_ImportExpanded)
        {
            if (GUILayout.Button("Select heightmap image"))
            {
                string previousImageFilePath = HeightMapTool_SelectedImageFilePath;
                HeightMapTool_SelectedImageFilePath = EditorUtility.OpenFilePanelWithFilters("Select Image",
                                                       Application.dataPath,
                                                       new string[] {"Image files", "png,jpg,jpeg",
                                                                 "All files", "*"});

                HeightMapTool_SelectedImagePathValid = System.IO.File.Exists(HeightMapTool_SelectedImageFilePath);

                if (HeightMapTool_SelectedImagePathValid)
                {
                    // has the image changed?
                    if (previousImageFilePath != HeightMapTool_SelectedImageFilePath)
                    {
                        // if we have no previous image - create a temporary one
                        if (HeightMapTool_SelectedImage == null)
                            HeightMapTool_SelectedImage = new Texture2D(1, 1);

                        HeightMapTool_SelectedImage.LoadImage(System.IO.File.ReadAllBytes(HeightMapTool_SelectedImageFilePath));
                        HeightMapTool_SelectedImage.wrapMode = TextureWrapMode.Clamp;
                    }
                }
                else
                    HeightMapTool_SelectedImage = null;
            }

            if (HeightMapTool_SelectedImagePathValid)
            {
                EditorGUILayout.LabelField($"Selected file: {System.IO.Path.GetFileName(HeightMapTool_SelectedImageFilePath)}");
                EditorGUILayout.LabelField($"Width: {HeightMapTool_SelectedImage.width} Height: {HeightMapTool_SelectedImage.height}");

                // image is not square - exit
                if (HeightMapTool_SelectedImage.width != HeightMapTool_SelectedImage.height)
                {
                    EditorGUILayout.LabelField("Image must be square", EditorStyles.boldLabel);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                HeightMapTool_ImportMode = (EHeightMapImportMode)EditorGUILayout.EnumPopup("Import Mode", HeightMapTool_ImportMode);

                // are we trying to import an invalid image
                if (!System.Array.Exists(ValidHeightMapResolutions, element => element == HeightMapTool_SelectedImage.width))
                {
                    if (HeightMapTool_ImportMode == EHeightMapImportMode.ResizeTerrainHeightMapToMatchImage)
                    {
                        EditorGUILayout.LabelField("Image resolution must be in valid range", EditorStyles.boldLabel);
                        EditorGUILayout.EndFoldoutHeaderGroup();
                        return;

                    }
                }

                HeightMapTool_Intensity = EditorGUILayout.Slider("Intensity", HeightMapTool_Intensity, 0f, 1f);

                if (GUILayout.Button("Import Heightmap"))
                {
                    ImportHeightMap(SelectedTerrain, HeightMapTool_SelectedImage, HeightMapTool_ImportMode, HeightMapTool_Intensity);
                }
            }
            else
                EditorGUILayout.LabelField("No file selected");

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        HeightMapTool_ExportExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(HeightMapTool_ExportExpanded, "Export Height Map");

        if (HeightMapTool_ExportExpanded)
        {
            if (GUILayout.Button("Export Height Map"))
            {
                ExportHeightMap(SelectedTerrain);
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void ImportHeightMap(Terrain targetTerrain, Texture2D heightMap, EHeightMapImportMode importMode, float intensity)
    {
        if (importMode == EHeightMapImportMode.ResizeTerrainHeightMapToMatchImage ||
            heightMap.width == targetTerrain.terrainData.heightmapResolution)
        {
            float[,] heights = new float[heightMap.width, heightMap.height];
            Color[] pixelData = heightMap.GetPixels();

            for(int y = 0; y < heightMap.height; ++y)
            {
                for(int x = 0; x < heightMap.width; ++x)
                {
                    int pixelIndex = x + y * heightMap.width;

                    heights[x, y] = pixelData[pixelIndex].grayscale * intensity;
                }
            }

            if (targetTerrain.terrainData.heightmapResolution != heightMap.width)
            {
                Vector3 currentSize = targetTerrain.terrainData.size;
                targetTerrain.terrainData.heightmapResolution = heightMap.width;
                targetTerrain.terrainData.size = currentSize;
            }

            targetTerrain.terrainData.SetHeights(0, 0, heights);
        }
        else
        {
            int heightMapResolution = targetTerrain.terrainData.heightmapResolution;
            float[,] heights = new float[heightMapResolution, heightMapResolution];

            for (int y = 0; y < heightMapResolution; ++y)
            {
                float heightMapV = (float)y / (float)(heightMapResolution - 1);

                for(int x = 0; x < heightMapResolution; ++x)
                {
                    float heightMapU = (float)x / (float)(heightMapResolution - 1);

                    Color pixel = heightMap.GetPixelBilinear(heightMapU, heightMapV);

                    heights[x, y] = pixel.grayscale * intensity;
                }
            }

            targetTerrain.terrainData.SetHeights(0, 0, heights);
        }
    }

    void ExportHeightMap(Terrain targetTerrain)
    {
        int heightMapResolution = targetTerrain.terrainData.heightmapResolution;

        float[,] heights = targetTerrain.terrainData.GetHeights(0, 0, heightMapResolution, heightMapResolution);
        Color[] pixelData = new Color[heightMapResolution * heightMapResolution];

        for (int y = 0; y < heightMapResolution; ++y)
        {
            for (int x = 0; x < heightMapResolution; ++x)
            {
                int pixelIndex = x + y * heightMapResolution;

                float height = heights[x, y];
                pixelData[pixelIndex] = new Color(height, height, height);
            }
        }

        Texture2D heightMap = new Texture2D(heightMapResolution, heightMapResolution, TextureFormat.RGB24, false);
        heightMap.SetPixels(pixelData);

        string saveFilePath = EditorUtility.SaveFilePanel("Save heightmap as ...", Application.dataPath, "TerrainHeightMap.png", "png");
        if (saveFilePath.Length > 0)
        {
            System.IO.File.WriteAllBytes(saveFilePath, heightMap.EncodeToPNG());
        }
    }
}
