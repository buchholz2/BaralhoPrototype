#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ChalkSetupUtility
{
    private const string ChalkRoot = "Assets/Chalk";
    private const string OnlyGfxRoot = "Assets/Chalk/OnlyGFX";
    private const string TexturelabsRoot = "Assets/Chalk/Texturelabs/InkPaint_319";
    private const string MaterialsRoot = "Assets/Chalk/Materials";
    private const string OverlayMaterialPath = "Assets/Chalk/Materials/Mat_ChalkOverlay.mat";
    private const string TmpMaterialPath = "Assets/Chalk/Materials/Mat_ChalkTMP.mat";
    private const string DemoBgMaterialPath = "Assets/Chalk/Materials/Mat_ChalkDemoBackground.mat";
    private const string DemoScenePath = "Assets/Scenes/ChalkDemo.unity";
    private const string ChalkSortingLayer = "ChalkOverlay";

    [MenuItem("Tools/Chalk/Run Setup")]
    public static void RunSetup()
    {
        EnsureFolders();
        SyncExternalAssets();
        AssetDatabase.Refresh();

        Texture2D strokeTexture = FindBestStrokeTexture(out string strokePath);
        Texture2D grainTexture = FindBestGrainTexture(out string grainPath);

        if (!string.IsNullOrEmpty(strokePath))
            ConfigureStrokeImporter(strokePath);
        if (!string.IsNullOrEmpty(grainPath))
            ConfigureGrainImporter(grainPath);

        AssetDatabase.Refresh();

        EnsureSortingLayer(ChalkSortingLayer);
        Material overlayMat = CreateOrUpdateOverlayMaterial(strokeTexture, grainTexture);
        Material tmpMat = CreateOrUpdateTmpMaterial(grainTexture);

        AssetDatabase.SaveAssets();
        Debug.Log($"Chalk setup completo. Pipeline: {GetPipelineLabel()} | Stroke: {strokePath} | Grain: {grainPath} | OverlayMat: {overlayMat != null} | TMPMat: {tmpMat != null}");
    }

    [MenuItem("Tools/Chalk/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        RunSetup();

        Scene demoScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.6f;
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.10f, 0.55f, 0.34f, 1f);
        }

        Material overlayMat = AssetDatabase.LoadAssetAtPath<Material>(OverlayMaterialPath);
        Material tmpMat = AssetDatabase.LoadAssetAtPath<Material>(TmpMaterialPath);
        Texture2D grainTex = overlayMat != null && overlayMat.HasProperty("_GrainTex")
            ? overlayMat.GetTexture("_GrainTex") as Texture2D
            : AssetDatabase.LoadAssetAtPath<Texture2D>(FindBestGrainTexturePath());

        CreateDemoBackground();

        GameObject root = new GameObject("ChalkDemo");
        ChalkDemoBuilder builder = root.AddComponent<ChalkDemoBuilder>();
        builder.chalkOverlayMaterial = overlayMat;
        builder.chalkTextMaterial = tmpMat;
        builder.grainTexture = grainTex;
        builder.opacity = 0.24f;
        builder.grainStrength = 0.62f;
        builder.chalkTint = new Color(0.93f, 0.91f, 0.86f, 0.9f);
        builder.baseSortingOrder = 20;
        builder.BuildDemo();

        EditorSceneManager.MarkSceneDirty(demoScene);
        EditorSceneManager.SaveScene(demoScene, DemoScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Cena demo criada: {DemoScenePath}");
    }

    private static string GetPipelineLabel()
    {
        return GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.GetType().Name;
    }

    private static void EnsureFolders()
    {
        EnsureFolder(ChalkRoot);
        EnsureFolder(OnlyGfxRoot);
        EnsureFolder(TexturelabsRoot);
        EnsureFolder(MaterialsRoot);
        EnsureFolder("Assets/Editor/Chalk");
        EnsureFolder("Assets/Scripts/Chalk");
    }

    private static void EnsureFolder(string assetRelativePath)
    {
        string absolutePath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), assetRelativePath);
        absolutePath = absolutePath.Replace('/', Path.DirectorySeparatorChar);
        if (!Directory.Exists(absolutePath))
            Directory.CreateDirectory(absolutePath);
    }

    private static void SyncExternalAssets()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string externalStrokeFolder = Path.Combine(projectRoot, "chalk-line-strokes");
        string targetStrokeFolder = Path.Combine(projectRoot, OnlyGfxRoot);
        if (Directory.Exists(externalStrokeFolder))
            CopyDirectory(externalStrokeFolder, targetStrokeFolder);

        string[] grainCandidates =
        {
            "Texturelabs_InkPaint_319XL.jpg",
            "Texturelabs_InkPaint_319L.jpg",
            "Texturelabs_InkPaint_319M.jpg",
            "Texturelabs_InkPaint_319S.jpg",
            "Texturelabs_InkPaint_319XL.png",
            "Texturelabs_InkPaint_319L.png",
            "Texturelabs_InkPaint_319M.png",
            "Texturelabs_InkPaint_319S.png"
        };

        string targetGrainFolder = Path.Combine(projectRoot, TexturelabsRoot);
        foreach (string fileName in grainCandidates)
        {
            string sourcePath = Path.Combine(projectRoot, fileName);
            if (!File.Exists(sourcePath))
                continue;

            string destinationPath = Path.Combine(targetGrainFolder, fileName);
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            if (string.IsNullOrEmpty(fileName))
                continue;
            if (fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;
            File.Copy(file, Path.Combine(destinationDir, fileName), true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            if (string.IsNullOrEmpty(dirName))
                continue;
            CopyDirectory(subDir, Path.Combine(destinationDir, dirName));
        }
    }

    private static Texture2D FindBestStrokeTexture(out string bestPath)
    {
        bestPath = null;
        float bestScore = float.MinValue;
        Texture2D bestTexture = null;

        string[] searchFolders = { OnlyGfxRoot, ChalkRoot };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (name.Contains("cover") || name.Contains("license") || name.Contains("licence"))
                continue;
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                continue;

            float score = 0f;
            if (path.ToLowerInvariant().Contains("onlygfx")) score += 150f;
            if (name.StartsWith("l-")) score += 120f;
            if (name.Contains("stroke") || name.Contains("line")) score += 90f;
            if (name.Contains("tile") || name.Contains("repeat") || name.Contains("seamless")) score += 130f;

            float ratio = tex.height > 0 ? (float)tex.width / tex.height : 1f;
            if (ratio > 1.5f && ratio < 20f) score += 70f;

            score += Mathf.Min((tex.width * tex.height) / 4000f, 800f);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.DoesSourceTextureHaveAlpha())
                score += 40f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTexture = tex;
                bestPath = path;
            }
        }

        return bestTexture;
    }

    private static Texture2D FindBestGrainTexture(out string bestPath)
    {
        bestPath = null;
        float bestScore = float.MinValue;
        Texture2D bestTexture = null;

        string[] searchFolders = { TexturelabsRoot, ChalkRoot };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            string lowerPath = path.ToLowerInvariant();
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            bool looksLikeGrain = lowerPath.Contains("inkpaint_319") || lowerPath.Contains("texturelabs") || name.Contains("grain") || name.Contains("chalk");
            if (!looksLikeGrain)
                continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                continue;

            float score = 0f;
            if (lowerPath.Contains("inkpaint_319")) score += 220f;
            if (name.Contains("xl")) score += 220f;
            else if (name.EndsWith("l")) score += 160f;
            else if (name.EndsWith("m")) score += 100f;
            else if (name.EndsWith("s")) score += 60f;
            if (name.Contains("tile") || name.Contains("repeat") || name.Contains("seamless")) score += 150f;

            score += Mathf.Min((tex.width * tex.height) / 3000f, 1200f);

            if (score > bestScore)
            {
                bestScore = score;
                bestTexture = tex;
                bestPath = path;
            }
        }

        return bestTexture;
    }

    private static string FindBestGrainTexturePath()
    {
        FindBestGrainTexture(out string path);
        return path;
    }

    private static void ConfigureStrokeImporter(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 4096);
        importer.SaveAndReimport();
    }

    private static void ConfigureGrainImporter(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.sRGBTexture = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 4096);
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateOverlayMaterial(Texture2D strokeTexture, Texture2D grainTexture)
    {
        Shader shader = Shader.Find("Chalk/Overlay");
        if (shader == null)
        {
            Debug.LogError("Shader 'Chalk/Overlay' nao encontrado.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(OverlayMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Mat_ChalkOverlay" };
            AssetDatabase.CreateAsset(material, OverlayMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        if (strokeTexture != null && material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", strokeTexture);
        if (grainTexture != null && material.HasProperty("_GrainTex"))
            material.SetTexture("_GrainTex", grainTexture);

        if (material.HasProperty("_Opacity")) material.SetFloat("_Opacity", 0.24f);
        if (material.HasProperty("_GrainStrength")) material.SetFloat("_GrainStrength", 0.62f);
        if (material.HasProperty("_GrainScale")) material.SetVector("_GrainScale", new Vector4(5f, 5f, 0f, 0f));
        if (material.HasProperty("_ChalkTint")) material.SetColor("_ChalkTint", new Color(0.93f, 0.91f, 0.86f, 0.9f));

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrUpdateTmpMaterial(Texture2D grainTexture)
    {
        Shader shader = Shader.Find("Chalk/TMP SDF Overlay");
        if (shader == null)
        {
            Debug.LogError("Shader 'Chalk/TMP SDF Overlay' nao encontrado.");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
        {
            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids.Length > 0)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        Material source = fontAsset != null ? fontAsset.material : null;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TmpMaterialPath);
        if (material == null)
        {
            material = source != null ? new Material(source) : new Material(shader);
            material.name = "Mat_ChalkTMP";
            AssetDatabase.CreateAsset(material, TmpMaterialPath);
        }
        else if (source != null)
        {
            material.CopyPropertiesFromMaterial(source);
        }

        material.shader = shader;

        if (source != null && source.HasProperty("_MainTex") && material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", source.GetTexture("_MainTex"));
        if (grainTexture != null && material.HasProperty("_GrainTex"))
            material.SetTexture("_GrainTex", grainTexture);
        if (material.HasProperty("_Opacity")) material.SetFloat("_Opacity", 0.24f);
        if (material.HasProperty("_GrainStrength")) material.SetFloat("_GrainStrength", 0.62f);
        if (material.HasProperty("_GrainScale")) material.SetVector("_GrainScale", new Vector4(5f, 5f, 0f, 0f));
        if (material.HasProperty("_ChalkTint")) material.SetColor("_ChalkTint", new Color(0.93f, 0.91f, 0.86f, 0.9f));
        if (material.HasProperty("_FaceColor")) material.SetColor("_FaceColor", Color.white);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateDemoBackground()
    {
        Shader bgShader = GraphicsSettings.currentRenderPipeline == null
            ? Shader.Find("Unlit/Color")
            : Shader.Find("Universal Render Pipeline/Unlit");

        if (bgShader == null)
            bgShader = Shader.Find("Unlit/Color");

        Material bgMat = AssetDatabase.LoadAssetAtPath<Material>(DemoBgMaterialPath);
        if (bgMat == null)
        {
            bgMat = new Material(bgShader) { name = "Mat_ChalkDemoBackground" };
            AssetDatabase.CreateAsset(bgMat, DemoBgMaterialPath);
        }
        else
        {
            bgMat.shader = bgShader;
        }

        Color feltColor = new Color(0.09f, 0.56f, 0.35f, 1f);
        if (bgMat.HasProperty("_BaseColor"))
            bgMat.SetColor("_BaseColor", feltColor);
        else if (bgMat.HasProperty("_Color"))
            bgMat.SetColor("_Color", feltColor);

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "GreenFeltBackground";
        background.transform.position = new Vector3(0f, 0f, 5f);
        background.transform.localScale = new Vector3(18f, 10f, 1f);
        var renderer = background.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = bgMat;
        UnityEngine.Object.DestroyImmediate(background.GetComponent<Collider>());
    }

    private static void EnsureSortingLayer(string layerName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
            return;

        var tagManager = new SerializedObject(assets[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
        if (sortingLayers == null)
            return;

        for (int i = 0; i < sortingLayers.arraySize; i++)
        {
            SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = layer.FindPropertyRelative("name");
            if (nameProp != null && nameProp.stringValue == layerName)
                return;
        }

        sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
        SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
        SerializedProperty newName = newLayer.FindPropertyRelative("name");
        if (newName != null)
            newName.stringValue = layerName;

        SerializedProperty uniqueId = newLayer.FindPropertyRelative("uniqueID");
        if (uniqueId != null)
            uniqueId.intValue = GenerateSortingLayerId(sortingLayers);

        SerializedProperty locked = newLayer.FindPropertyRelative("locked");
        if (locked != null)
        {
            if (locked.propertyType == SerializedPropertyType.Boolean)
                locked.boolValue = false;
            else
                locked.intValue = 0;
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"Sorting Layer criada: {layerName}");
    }

    private static int GenerateSortingLayerId(SerializedProperty sortingLayers)
    {
        var used = new HashSet<int>();
        for (int i = 0; i < sortingLayers.arraySize; i++)
        {
            SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(i);
            SerializedProperty idProp = layer.FindPropertyRelative("uniqueID");
            if (idProp != null)
                used.Add(idProp.intValue);
        }

        int id = Guid.NewGuid().GetHashCode();
        while (id == 0 || used.Contains(id))
            id = id * 1103515245 + 12345;
        return id;
    }
}
#endif

