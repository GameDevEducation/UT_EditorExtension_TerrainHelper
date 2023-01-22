using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;

public class TerrainHelperEditorWindow : EditorWindow
{
    enum EHeightMapImportMode
    {
        FitImageToTerrainHeightMap,
        ResizeTerrainHeightMapToMatchImage
    }

    enum ETextureResolution
    {
        Resolution_512x512 = 512,
        Resolution_1024x1024 = 1024,
        Resolution_2048x2048 = 2048,
        Resolution_4096x4096 = 4096,
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

        TerrainTool_ConvertToMeshExpanded = true;
        TerrainTool_TextureResolution = ETextureResolution.Resolution_2048x2048;
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

    bool TerrainTool_ConvertToMeshExpanded = true;
    ETextureResolution TerrainTool_TextureResolution = ETextureResolution.Resolution_2048x2048;

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

        TerrainTool_ConvertToMeshExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(TerrainTool_ConvertToMeshExpanded, "Convert to Mesh");

        if (TerrainTool_ConvertToMeshExpanded)
        {
            TerrainTool_TextureResolution = (ETextureResolution)EditorGUILayout.EnumPopup("Texture Resolution", TerrainTool_TextureResolution);

            if (GUILayout.Button("Convert Terrain"))
            {
                ConvertToMesh(SelectedTerrain, TerrainTool_TextureResolution);
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

    void ConvertToMesh(Terrain targetTerrain, ETextureResolution textureResolution)
    {
        int heightMapResolution = targetTerrain.terrainData.heightmapResolution;

        float[,] heights = targetTerrain.terrainData.GetHeights(0, 0, heightMapResolution, heightMapResolution);

        Vector3[] vertices = new Vector3[heightMapResolution * heightMapResolution];
        Vector2[] uvCoordinates = new Vector2[heightMapResolution * heightMapResolution];
        int[] triangleIndices = new int[heightMapResolution * heightMapResolution * 3 * 2];

        // generate the mesh data
        for (int y = 0; y < heightMapResolution; y++) 
        { 
            for(int x = 0; x < heightMapResolution; x++) 
            {
                int vertIndex = x + y * heightMapResolution;

                vertices[vertIndex] = new Vector3(x * targetTerrain.terrainData.heightmapScale.x,
                                                  heights[y, x] * targetTerrain.terrainData.heightmapScale.y,
                                                  y * targetTerrain.terrainData.heightmapScale.z);
                uvCoordinates[vertIndex] = new Vector2((float)x / (heightMapResolution - 1f), 
                                                       (float)y / (heightMapResolution - 1f));

                if ((x < (heightMapResolution - 1)) && (y < (heightMapResolution -1)))
                {
                    triangleIndices[(vertIndex * 6) + 0] = vertIndex;
                    triangleIndices[(vertIndex * 6) + 1] = vertIndex + heightMapResolution;
                    triangleIndices[(vertIndex * 6) + 2] = vertIndex + heightMapResolution + 1;
                    triangleIndices[(vertIndex * 6) + 3] = vertIndex;
                    triangleIndices[(vertIndex * 6) + 4] = vertIndex + heightMapResolution + 1;
                    triangleIndices[(vertIndex * 6) + 5] = vertIndex + 1;
                }
            }
        }

        string baseAssetName = targetTerrain.name;
        string scenePath = System.IO.Path.GetDirectoryName(SceneManager.GetActiveScene().path);

        // create the mesh asset
        Mesh generatedMesh = new Mesh();
        generatedMesh.indexFormat = triangleIndices.Length > System.UInt16.MaxValue ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetUVs(0, uvCoordinates);
        generatedMesh.SetTriangles(triangleIndices, 0);
        generatedMesh.RecalculateBounds();
        generatedMesh.RecalculateNormals();

        // save the mesh asset
        string meshPath = System.IO.Path.Combine(scenePath, $"{baseAssetName}_Mesh.asset");
        meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
        AssetDatabase.CreateAsset(generatedMesh, meshPath);
        AssetDatabase.SaveAssets();

        // setup the texture capture camera
        GameObject textureCaptureGO = new GameObject("Capture Camera");
        textureCaptureGO.transform.SetParent(targetTerrain.transform);
        textureCaptureGO.transform.position = new Vector3(targetTerrain.terrainData.size.x / 2,
                                                          targetTerrain.terrainData.size.y + 25f,
                                                          targetTerrain.terrainData.size.z / 2);
        textureCaptureGO.transform.localEulerAngles = new Vector3(90f, 0, 0);

        Camera captureCamera = textureCaptureGO.AddComponent<Camera>();
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = Color.black;
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = targetTerrain.terrainData.size.x / 2;
        Undo.RegisterCreatedObjectUndo(textureCaptureGO, "Setup capture camera");

        bool oldDrawTreesAndFoliage = targetTerrain.drawTreesAndFoliage;
        targetTerrain.drawTreesAndFoliage = false;

        // setup render texture and capture the image
        RenderTexture captureRT = new RenderTexture((int)textureResolution, (int)textureResolution, 24);
        captureCamera.targetTexture = captureRT;

        Texture2D capturedTexture = new Texture2D(captureRT.width, captureRT.height, TextureFormat.RGB24, false);
        captureCamera.Render();
        RenderTexture.active = captureRT;
        capturedTexture.ReadPixels(new Rect(0, 0, captureRT.width, captureRT.height), 0, 0);

        // cleanup
        targetTerrain.drawTreesAndFoliage = oldDrawTreesAndFoliage;
        RenderTexture.active = null;
        captureCamera.targetTexture = null;
        DestroyImmediate(captureRT);
        DestroyImmediate(textureCaptureGO, true);

        // save the texture asset
        string texturePath = System.IO.Path.Combine(scenePath, $"{baseAssetName}_Texture.png");
        texturePath = AssetDatabase.GenerateUniqueAssetPath(texturePath);
        File.WriteAllBytes(texturePath, capturedTexture.EncodeToPNG());
        AssetDatabase.Refresh();

        // load the texture asset
        Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        // create the material
        Material terrainMaterial = new Material(Shader.Find("Standard"));
        terrainMaterial.SetTexture("_MainTex", textureAsset);

        // save the material asset
        string materialPath = System.IO.Path.Combine(scenePath, $"{baseAssetName}_Mat.mat");
        materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);
        AssetDatabase.CreateAsset(terrainMaterial, materialPath);
        AssetDatabase.SaveAssets();

        // setup the game object
        GameObject terrainGO = new GameObject($"{targetTerrain.name}_Mesh");
        MeshFilter meshFilter = terrainGO.AddComponent<MeshFilter>();
        meshFilter.mesh = generatedMesh;
        MeshRenderer meshRenderer = terrainGO.AddComponent<MeshRenderer>();
        meshRenderer.SetMaterials(new List<Material> { terrainMaterial });
        Undo.RegisterCreatedObjectUndo(terrainGO, "Created mesh game object");

        // save the prefab asset
        string prefabPath = System.IO.Path.Combine(scenePath, $"{baseAssetName}.prefab");
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        
        bool prefabCreated = false;
        GameObject prefabGO = PrefabUtility.SaveAsPrefabAsset(terrainGO, prefabPath, out prefabCreated);
        DestroyImmediate(terrainGO, true);

        if (prefabCreated)
        {
            GameObject meshGO = PrefabUtility.InstantiatePrefab(prefabGO) as GameObject;

            meshGO.transform.position = targetTerrain.transform.position;
            meshGO.transform.rotation = targetTerrain.transform.rotation;

            Undo.RegisterCreatedObjectUndo(meshGO, "Created terrain mesh");
        }
    }
}
