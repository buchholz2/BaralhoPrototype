using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class UiAssetPostprocessor : AssetPostprocessor
{
    private const string UiSourcePath = "Assets/Art/UI/Source/";
    private const string UiSpritesPath = "Assets/UI/Sprites/";

    private struct SheetInfo
    {
        public int Columns;
        public int Rows;
        public int Border;
        public string Prefix;
        public bool UseStateNames;
        public bool AllowManual;
        public bool UseAlphaTight;
        public int AlphaPadding;
        public byte AlphaThreshold;
        public bool VerticalOnly;
        public int MinHeight;
        public bool ForceSquare;
        public bool PreferLargestComponent;

        public SheetInfo(
            int columns,
            int rows,
            int border,
            string prefix,
            bool useStateNames,
            bool allowManual = false,
            bool useAlphaTight = false,
            int alphaPadding = 4,
            byte alphaThreshold = 10,
            bool verticalOnly = false,
            int minHeight = 0,
            bool forceSquare = false,
            bool preferLargestComponent = false)
        {
            Columns = columns;
            Rows = rows;
            Border = border;
            Prefix = prefix;
            UseStateNames = useStateNames;
            AllowManual = allowManual;
            UseAlphaTight = useAlphaTight;
            AlphaPadding = alphaPadding;
            AlphaThreshold = alphaThreshold;
            VerticalOnly = verticalOnly;
            MinHeight = minHeight;
            ForceSquare = forceSquare;
            PreferLargestComponent = preferLargestComponent;
        }
    }

    private static readonly Dictionary<string, SheetInfo> Sheets = new()
    {
        // Use alpha-tight slicing for button sheets to avoid empty background.
        { "btn_main_sheet", new SheetInfo(4, 1, 48, "Main", true, false, true, 60, 10, true, 440) },
        { "btn_icon_square_sheet", new SheetInfo(4, 1, 64, "IconSquare", true, false, true, 50, 10, true, 360) },
        { "frame_panel", new SheetInfo(4, 1, 70, "FramePanel", false, false, true, 24, 10, true, 260) },
        // Auto slice with alpha-tight bounds (similar to Outline tool).
        { "icons_gold_sheet", new SheetInfo(4, 2, 0, "GoldIcon", false, false, true, 4, 129, false, 0) },
        // Singles treated as 1x1 sheets for alpha-tight crop
        { "btn_pill", new SheetInfo(1, 1, 70, "Pill", false, false, true, 60, 10, false, 0) },
        { "highlight_hover", new SheetInfo(1, 1, 60, "Highlight", false, false, true, 60, 10, false, 0) },
        { "sort_square_sheet", new SheetInfo(1, 1, 0, "SortSquare", false, false, true, 12, 2, false, 0, true) },
        // Glyph sheets (ranks/suits)
        { "suits_all", new SheetInfo(4, 1, 0, "Suit", false, false, true, 4, 129, false, 0, false, true) },
        { "ranks_1_4", new SheetInfo(2, 2, 0, "Rank", false, false, true, 4, 129, false, 0, false, true) },
        { "ranks_5_8", new SheetInfo(2, 2, 0, "Rank", false, false, true, 4, 129, false, 0, false, true) },
        { "ranks_9_10_J_Q", new SheetInfo(2, 2, 0, "Rank", false, false, true, 4, 129, false, 0, false, true) },
        { "ranks_K_A_Joker_Plus", new SheetInfo(2, 2, 0, "Rank", false, false, true, 4, 129, false, 0, false, true) },
    };

    private static readonly HashSet<string> Singles = new();

    private void OnPreprocessTexture()
    {
        if (!IsUiAsset(assetPath))
            return;

        var importer = (TextureImporter)assetImporter;
        var name = Path.GetFileNameWithoutExtension(assetPath);

        // Configuracao padrao para UI (sprites nitidos e com alpha)
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100f;

        if (Sheets.TryGetValue(name, out var info))
        {
            // Spritesheet com estados (grid fixo)
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spriteBorder = Vector4.zero;
            if (info.AllowManual && HasExistingSpriteRects(importer))
                return;

            TryGetTextureSizeForImport(importer, assetPath, out var texW, out var texH);
            var metas = BuildMetasForSheet(name, assetPath, texW, texH, info);
            if (!TryApplyImporterSpriteRects(importer, metas))
                Debug.LogWarning($"UiAssetPostprocessor: preprocess falhou ao aplicar sprite rects em '{assetPath}'.");
            return;
        }

        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = Vector4.zero;
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsUiAsset(assetPath))
            return;

        var name = Path.GetFileNameWithoutExtension(assetPath);
        if (!Sheets.TryGetValue(name, out var info))
            return;

        var importer = (TextureImporter)assetImporter;
        if (info.AllowManual && HasExistingSpriteRects(importer))
            return;

        TryGetTextureSizeForImport(importer, assetPath, out var texW, out var texH);
        var metas = BuildMetasForSheet(name, assetPath, texW, texH, info);

        if (HasExpectedSpriteRects(importer, metas))
            return;

        if (!TryApplyImporterSpriteRects(importer, metas))
            Debug.LogWarning($"UiAssetPostprocessor: nao foi possivel aplicar sprite rects para '{assetPath}'.");
    }

    private static bool HasExistingSpriteRects(TextureImporter importer)
    {
        if (!TryGetSpriteRects(importer, out var rects))
            return false;
        return rects != null && rects.Length > 0;
    }

    private static bool TryGetSpriteRects(TextureImporter importer, out System.Array rects)
    {
        rects = null;
        if (TryGetProviderSpriteRects(importer, out rects))
            return true;
        if (TryGetLegacySpriteSheet(importer, out rects))
            return true;
        return TryGetSerializedSpriteSheet(importer, out rects);
    }

    private static bool TryGetProviderSpriteRects(TextureImporter importer, out System.Array rects)
    {
        rects = null;
        try
        {
            var factoryType = FindType("UnityEditor.U2D.Sprites.SpriteDataProviderFactory");
            if (factoryType == null)
                return false;

            var factory = System.Activator.CreateInstance(factoryType);
            factoryType.GetMethod("Init")?.Invoke(factory, null);
            var getProvider = factoryType.GetMethod("GetSpriteEditorDataProviderFromObject");
            if (getProvider == null)
                return false;

            var dataProvider = getProvider.Invoke(factory, new object[] { importer });
            if (dataProvider == null)
                return false;

            dataProvider.GetType().GetMethod("InitSpriteEditorDataProvider")?.Invoke(dataProvider, null);
            var getRects = dataProvider.GetType().GetMethod("GetSpriteRects");
            if (getRects == null)
                return false;

            rects = getRects.Invoke(dataProvider, null) as System.Array;
            return rects != null;
        }
        catch
        {
            rects = null;
            return false;
        }
    }

    private static bool TryApplyImporterSpriteRects(TextureImporter importer, SpriteMetaData[] metas)
    {
        if (TryApplySpriteRects(importer, metas))
            return true;
        if (TryApplyLegacySpriteSheet(importer, metas))
            return true;
        return TryApplySerializedSpriteSheet(importer, metas);
    }

    private static bool TryApplySpriteRects(TextureImporter importer, SpriteMetaData[] metas)
    {
        try
        {
            var factoryType = FindType("UnityEditor.U2D.Sprites.SpriteDataProviderFactory");
            if (factoryType == null)
                return false;

            var factory = System.Activator.CreateInstance(factoryType);
            factoryType.GetMethod("Init")?.Invoke(factory, null);
            var getProvider = factoryType.GetMethod("GetSpriteEditorDataProviderFromObject");
            if (getProvider == null)
                return false;

            var dataProvider = getProvider.Invoke(factory, new object[] { importer });
            if (dataProvider == null)
                return false;

            dataProvider.GetType().GetMethod("InitSpriteEditorDataProvider")?.Invoke(dataProvider, null);

            var spriteRectType = FindType("UnityEditor.U2D.Sprites.SpriteRect");
            if (spriteRectType == null)
                return false;

            var rects = System.Array.CreateInstance(spriteRectType, metas.Length);
            for (int i = 0; i < metas.Length; i++)
            {
                var meta = metas[i];
                var rect = System.Activator.CreateInstance(spriteRectType);
                spriteRectType.GetProperty("name")?.SetValue(rect, meta.name);
                spriteRectType.GetProperty("rect")?.SetValue(rect, meta.rect);
                spriteRectType.GetProperty("alignment")?.SetValue(rect, (SpriteAlignment)meta.alignment);
                spriteRectType.GetProperty("border")?.SetValue(rect, meta.border);
                spriteRectType.GetProperty("pivot")?.SetValue(rect, meta.pivot);

                var idProp = spriteRectType.GetProperty("spriteID");
                if (idProp != null && idProp.PropertyType == typeof(GUID))
                    idProp.SetValue(rect, GUID.Generate());

                rects.SetValue(rect, i);
            }

            var setRects = dataProvider.GetType().GetMethod("SetSpriteRects");
            var apply = dataProvider.GetType().GetMethod("Apply");
            if (setRects == null || apply == null)
                return false;

            setRects.Invoke(dataProvider, new object[] { rects });
            apply.Invoke(dataProvider, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryApplyLegacySpriteSheet(TextureImporter importer, SpriteMetaData[] metas)
    {
        if (importer == null || metas == null)
            return false;

        var prop = importer.GetType().GetProperty("spritesheet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null || !prop.CanWrite)
            return false;

        try
        {
            prop.SetValue(importer, metas, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetLegacySpriteSheet(TextureImporter importer, out System.Array sprites)
    {
        sprites = null;
        if (importer == null)
            return false;

        var prop = importer.GetType().GetProperty("spritesheet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null || !prop.CanRead)
            return false;

        try
        {
            sprites = prop.GetValue(importer, null) as System.Array;
            return sprites != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryApplySerializedSpriteSheet(TextureImporter importer, SpriteMetaData[] metas)
    {
        if (importer == null || metas == null)
            return false;

        try
        {
            var so = new SerializedObject(importer);
            var spritesProp = FindFirstProperty(so, "m_SpriteSheet.m_Sprites", "m_SpriteSheet.sprites", "spriteSheet.sprites");
            if (spritesProp == null || !spritesProp.isArray)
                return false;

            spritesProp.arraySize = metas.Length;
            for (int i = 0; i < metas.Length; i++)
            {
                var meta = metas[i];
                var entry = spritesProp.GetArrayElementAtIndex(i);
                SetPropertyValue(entry, meta);
            }

            var modeProp = FindFirstProperty(so, "m_SpriteMode", "spriteMode");
            if (modeProp != null)
                modeProp.intValue = (int)SpriteImportMode.Multiple;

            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetSerializedSpriteSheet(TextureImporter importer, out System.Array sprites)
    {
        sprites = null;
        if (importer == null)
            return false;

        try
        {
            var so = new SerializedObject(importer);
            var spritesProp = FindFirstProperty(so, "m_SpriteSheet.m_Sprites", "m_SpriteSheet.sprites", "spriteSheet.sprites");
            if (spritesProp == null || !spritesProp.isArray)
                return false;

            var metas = new SpriteMetaData[spritesProp.arraySize];
            for (int i = 0; i < metas.Length; i++)
            {
                var entry = spritesProp.GetArrayElementAtIndex(i);
                metas[i] = GetPropertyValue(entry);
            }

            sprites = metas;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasExpectedSpriteRects(TextureImporter importer, SpriteMetaData[] expectedMetas)
    {
        if (!TryGetSpriteRects(importer, out var rects))
            return false;
        if (rects == null || rects.Length != expectedMetas.Length)
            return false;

        var expectedByName = new Dictionary<string, Rect>(expectedMetas.Length);
        for (int i = 0; i < expectedMetas.Length; i++)
            expectedByName[expectedMetas[i].name] = expectedMetas[i].rect;

        for (int i = 0; i < rects.Length; i++)
        {
            if (!TryExtractNameAndRect(rects.GetValue(i), out var name, out var rect))
                return false;
            if (!expectedByName.TryGetValue(name, out var expectedRect))
                return false;
            if (!ApproximatelyEqualRect(rect, expectedRect))
                return false;
        }

        return true;
    }

    private static bool TryExtractNameAndRect(object spriteRectOrMeta, out string name, out Rect rect)
    {
        name = null;
        rect = default;
        if (spriteRectOrMeta == null)
            return false;

        if (spriteRectOrMeta is SpriteMetaData meta)
        {
            name = meta.name;
            rect = meta.rect;
            return true;
        }

        var type = spriteRectOrMeta.GetType();
        var nameProp = type.GetProperty("name");
        var rectProp = type.GetProperty("rect");
        if (nameProp == null || rectProp == null)
            return false;

        name = nameProp.GetValue(spriteRectOrMeta) as string;
        var rectValue = rectProp.GetValue(spriteRectOrMeta);
        if (rectValue is Rect rectTyped)
        {
            rect = rectTyped;
            return !string.IsNullOrEmpty(name);
        }

        return false;
    }

    private static bool ApproximatelyEqualRect(Rect a, Rect b)
    {
        const float tolerance = 0.5f;
        return Mathf.Abs(a.x - b.x) <= tolerance &&
               Mathf.Abs(a.y - b.y) <= tolerance &&
               Mathf.Abs(a.width - b.width) <= tolerance &&
               Mathf.Abs(a.height - b.height) <= tolerance;
    }

    private static SerializedProperty FindFirstProperty(SerializedObject obj, params string[] paths)
    {
        if (obj == null || paths == null)
            return null;
        for (int i = 0; i < paths.Length; i++)
        {
            var prop = obj.FindProperty(paths[i]);
            if (prop != null)
                return prop;
        }
        return null;
    }

    private static SerializedProperty FindFirstRelativeProperty(SerializedProperty parent, params string[] names)
    {
        if (parent == null || names == null)
            return null;
        for (int i = 0; i < names.Length; i++)
        {
            var prop = parent.FindPropertyRelative(names[i]);
            if (prop != null)
                return prop;
        }
        return null;
    }

    private static void SetPropertyValue(SerializedProperty entry, SpriteMetaData meta)
    {
        var nameProp = FindFirstRelativeProperty(entry, "name", "m_Name");
        if (nameProp != null)
            nameProp.stringValue = meta.name;

        var rectProp = FindFirstRelativeProperty(entry, "rect", "m_Rect");
        if (rectProp != null)
            rectProp.rectValue = meta.rect;

        var alignProp = FindFirstRelativeProperty(entry, "alignment", "m_Alignment");
        if (alignProp != null)
            alignProp.intValue = meta.alignment;

        var pivotProp = FindFirstRelativeProperty(entry, "pivot", "m_Pivot");
        if (pivotProp != null)
            pivotProp.vector2Value = meta.pivot;

        var borderProp = FindFirstRelativeProperty(entry, "border", "m_Border");
        if (borderProp != null)
            borderProp.vector4Value = meta.border;

        var idProp = FindFirstRelativeProperty(entry, "spriteID", "m_SpriteID");
        if (idProp != null && idProp.propertyType == SerializedPropertyType.String && string.IsNullOrEmpty(idProp.stringValue))
            idProp.stringValue = GUID.Generate().ToString();
    }

    private static SpriteMetaData GetPropertyValue(SerializedProperty entry)
    {
        var nameProp = FindFirstRelativeProperty(entry, "name", "m_Name");
        var rectProp = FindFirstRelativeProperty(entry, "rect", "m_Rect");
        var alignProp = FindFirstRelativeProperty(entry, "alignment", "m_Alignment");
        var pivotProp = FindFirstRelativeProperty(entry, "pivot", "m_Pivot");
        var borderProp = FindFirstRelativeProperty(entry, "border", "m_Border");

        return new SpriteMetaData
        {
            name = nameProp != null ? nameProp.stringValue : string.Empty,
            rect = rectProp != null ? rectProp.rectValue : default,
            alignment = alignProp != null ? alignProp.intValue : (int)SpriteAlignment.Center,
            pivot = pivotProp != null ? pivotProp.vector2Value : new Vector2(0.5f, 0.5f),
            border = borderProp != null ? borderProp.vector4Value : Vector4.zero
        };
    }

    private static void TryGetTextureSizeForImport(TextureImporter importer, string path, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (importer != null)
            importer.GetSourceTextureWidthAndHeight(out width, out height);
        if (width > 0 && height > 0)
            return;

        if (string.IsNullOrEmpty(path))
            return;

        var resolvedPath = ResolveAssetFilePath(path);
        if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            return;

        var bytes = File.ReadAllBytes(resolvedPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (tex.LoadImage(bytes))
        {
            width = tex.width;
            height = tex.height;
        }
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static string ResolveAssetFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (Path.IsPathRooted(path))
            return path;

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, path);
        }

        return Path.GetFullPath(path);
    }

    private static System.Type FindType(string fullName)
    {
        var type = System.Type.GetType(fullName);
        if (type != null) return type;

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            type = asm.GetType(fullName);
            if (type != null) return type;
        }
        return null;
    }

    private static SpriteMetaData[] BuildMetasForSheet(string name, string path, int texW, int texH, SheetInfo info)
    {
        if (info.UseAlphaTight)
            return BuildAlphaTightGrid(path, texW, texH, info, false);
        if (name == "btn_main_sheet")
            return BuildMainButtonSheet(texW, texH, info);
        return BuildGrid(texW, texH, info);
    }

    private static SpriteMetaData[] BuildGrid(int texW, int texH, SheetInfo info)
    {
        var list = new List<SpriteMetaData>();
        int cellW = texW / info.Columns;
        int cellH = texH / info.Rows;

        for (int row = 0; row < info.Rows; row++)
        {
            for (int col = 0; col < info.Columns; col++)
            {
                string name;
                if (info.UseStateNames && info.Rows == 1 && info.Columns == 4)
                {
                    name = col switch
                    {
                        0 => $"{info.Prefix}_Normal",
                        1 => $"{info.Prefix}_Hover",
                        2 => $"{info.Prefix}_Pressed",
                        _ => $"{info.Prefix}_Disabled",
                    };
                }
                else
                {
                    int index = row * info.Columns + col;
                    name = $"{info.Prefix}_{index}";
                }

                // SpriteMetaData uses texture space with origin at bottom-left
                float x = col * cellW;
                float y = texH - (row + 1) * cellH;

                var meta = new SpriteMetaData
                {
                    name = name,
                    rect = new Rect(x, y, cellW, cellH),
                    alignment = (int)SpriteAlignment.Center,
                    border = new Vector4(info.Border, info.Border, info.Border, info.Border),
                };
                meta = AssignSpriteId(meta);

                list.Add(meta);
            }
        }

        return list.ToArray();
    }

    private static bool IsUiAsset(string path)
    {
        return path.StartsWith(UiSourcePath) || path.StartsWith(UiSpritesPath);
    }

    private static SpriteMetaData[] BuildMainButtonSheet(int texW, int texH, SheetInfo info)
    {
        var list = new List<SpriteMetaData>();
        int cellW = texW / info.Columns;

        // Crop the button area (remove the big background).
        // These values were tuned for btn_main_sheet.png (1536x1024).
        int cropTop = 190;
        int cropBottom = 518;
        int cropHeight = cropBottom - cropTop;
        float rectY = texH - cropBottom;

        for (int col = 0; col < info.Columns; col++)
        {
            string name = col switch
            {
                0 => $"{info.Prefix}_Normal",
                1 => $"{info.Prefix}_Hover",
                2 => $"{info.Prefix}_Pressed",
                _ => $"{info.Prefix}_Disabled",
            };

            float x = col * cellW;
            var meta = new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, rectY, cellW, cropHeight),
                alignment = (int)SpriteAlignment.Center,
                border = new Vector4(info.Border, info.Border, info.Border, info.Border),
            };
            meta = AssignSpriteId(meta);
            list.Add(meta);
        }

        return list.ToArray();
    }

    private static SpriteMetaData[] BuildAlphaTightGrid(string path, int texW, int texH, SheetInfo info, bool useMainButtonCrop)
    {
        var list = new List<SpriteMetaData>();
        int cellW = texW / info.Columns;
        int cellH = texH / info.Rows;

        var resolvedPath = ResolveAssetFilePath(path);
        if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            return BuildGrid(texW, texH, info);

        var bytes = File.ReadAllBytes(resolvedPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        tex.LoadImage(bytes);
        var pixels = tex.GetPixels32();

        byte alphaThreshold = info.AlphaThreshold;
        int padding = info.AlphaPadding;

        for (int row = 0; row < info.Rows; row++)
        {
            for (int col = 0; col < info.Columns; col++)
            {
                string name;
                if (info.UseStateNames && info.Rows == 1 && info.Columns == 4)
                {
                    name = col switch
                    {
                        0 => $"{info.Prefix}_Normal",
                        1 => $"{info.Prefix}_Hover",
                        2 => $"{info.Prefix}_Pressed",
                        _ => $"{info.Prefix}_Disabled",
                    };
                }
                else
                {
                    int index = row * info.Columns + col;
                    name = $"{info.Prefix}_{index}";
                }

                int cellX = col * cellW;
                int cellY = texH - (row + 1) * cellH;

                Rect baseRect;
                if (useMainButtonCrop)
                {
                    int cropTop = 190;
                    int cropBottom = 518;
                    int cropHeight = cropBottom - cropTop;
                    float cropRectY = texH - cropBottom;
                    baseRect = new Rect(cellX, cropRectY, cellW, cropHeight);
                }
                else
                {
                    baseRect = new Rect(cellX, cellY, cellW, cellH);
                }

                int baseX = Mathf.RoundToInt(baseRect.x);
                int baseY = Mathf.RoundToInt(baseRect.y);
                int baseW = Mathf.RoundToInt(baseRect.width);
                int baseH = Mathf.RoundToInt(baseRect.height);
                var cellRect = new RectInt(baseX, baseY, baseW, baseH);
                CustomOutlineAutoCutter.TryComputeCutRect(
                    pixels,
                    texW,
                    texH,
                    cellRect,
                    alphaThreshold,
                    padding,
                    info.VerticalOnly,
                    info.MinHeight,
                    info.ForceSquare,
                    info.PreferLargestComponent,
                    out var cutRect);

                var rect = new Rect(cutRect.x, cutRect.y, cutRect.width, cutRect.height);
                var border = ClampBorder(rect, info.Border);
                var meta = new SpriteMetaData
                {
                    name = name,
                    rect = rect,
                    alignment = (int)SpriteAlignment.Center,
                    border = border,
                };
                meta = AssignSpriteId(meta);

                list.Add(meta);
            }
        }

        UnityEngine.Object.DestroyImmediate(tex);
        return list.ToArray();
    }
    private static SpriteMetaData AssignSpriteId(SpriteMetaData meta)
    {
        var type = typeof(SpriteMetaData);
        var field = type.GetField("spriteID");
        if (field != null)
        {
            if (field.FieldType == typeof(GUID))
            {
                field.SetValueDirect(__makeref(meta), GUID.Generate());
            }
            else if (field.FieldType == typeof(string))
            {
                field.SetValueDirect(__makeref(meta), GUID.Generate().ToString());
            }
            return meta;
        }

        var prop = type.GetProperty("spriteID") ?? type.GetProperty("spriteId");
        if (prop != null && prop.CanWrite)
        {
            if (prop.PropertyType == typeof(GUID))
                prop.SetValue(meta, GUID.Generate());
            else if (prop.PropertyType == typeof(string))
                prop.SetValue(meta, GUID.Generate().ToString());
        }
        return meta;
    }

    private static Vector4 ClampBorder(Rect rect, int border)
    {
        float maxX = Mathf.Max(0f, rect.width * 0.5f - 1f);
        float maxY = Mathf.Max(0f, rect.height * 0.5f - 1f);
        float bx = Mathf.Min(border, maxX);
        float by = Mathf.Min(border, maxY);
        return new Vector4(bx, by, bx, by);
    }
}
