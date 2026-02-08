using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class UiAssetAutoSetup
{
    private const string PrefKey = "GoldUIAssets.SetupDone.v2";
    private const string LegacySourcePath = "Assets/Art/UI/Source";
    private const string LegacyPrefabPath = "Assets/Art/UI/Prefabs";
    private const string UiRootPath = "Assets/UI";
    private const string UiSpriteRoot = "Assets/UI/Sprites";
    private const string UiPrefabPath = "Assets/UI/Prefabs";
    private const string UiButtonsPath = "Assets/UI/Sprites/Buttons";
    private const string UiGlyphsSuitsPath = "Assets/UI/Sprites/Glyphs/Suits";
    private const string UiGlyphsRanksPath = "Assets/UI/Sprites/Glyphs/Ranks";
    private static readonly string[] SpriteRoots = { UiSpriteRoot, LegacySourcePath };

    static UiAssetAutoSetup()
    {
        EditorApplication.delayCall += TryAutoSetup;
    }

    [MenuItem("Tools/UI/Setup Gold UI Assets")]
    public static void Setup()
    {
        EnsureFolders();
        ReimportUiSprites();

        // Gera prefabs base com TMP e estados do botao
        BuildMainButton();
        BuildIconButton();
        BuildPillButton();
        BuildFramePanel();
        BuildHighlight();
        BuildCardGlyphButton();
        CleanupCardGlyphExamples();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void TryAutoSetup()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
            return;

        if (!SpritesReady())
        {
            EditorApplication.delayCall += TryAutoSetup;
            return;
        }

        Setup();
        EditorPrefs.SetBool(PrefKey, true);
    }

    private static bool SpritesReady()
    {
        return LoadSprites("btn_main_sheet").Any();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/Art/UI"))
            AssetDatabase.CreateFolder("Assets/Art", "UI");
        if (!AssetDatabase.IsValidFolder(LegacyPrefabPath))
            AssetDatabase.CreateFolder("Assets/Art/UI", "Prefabs");

        if (!AssetDatabase.IsValidFolder(UiRootPath))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(UiSpriteRoot))
            AssetDatabase.CreateFolder(UiRootPath, "Sprites");
        if (!AssetDatabase.IsValidFolder(UiButtonsPath))
            AssetDatabase.CreateFolder(UiSpriteRoot, "Buttons");
        if (!AssetDatabase.IsValidFolder("Assets/UI/Sprites/Glyphs"))
            AssetDatabase.CreateFolder(UiSpriteRoot, "Glyphs");
        if (!AssetDatabase.IsValidFolder(UiGlyphsSuitsPath))
            AssetDatabase.CreateFolder("Assets/UI/Sprites/Glyphs", "Suits");
        if (!AssetDatabase.IsValidFolder(UiGlyphsRanksPath))
            AssetDatabase.CreateFolder("Assets/UI/Sprites/Glyphs", "Ranks");
        if (!AssetDatabase.IsValidFolder(UiPrefabPath))
            AssetDatabase.CreateFolder(UiRootPath, "Prefabs");
    }

    private static void ReimportUiSprites()
    {
        var paths = new[]
        {
            "Assets/Art/UI/Source/btn_main_sheet.png",
            "Assets/Art/UI/Source/btn_icon_square_sheet.png",
            "Assets/Art/UI/Source/btn_pill.png",
            "Assets/Art/UI/Source/highlight_hover.png",
            "Assets/Art/UI/Source/frame_panel.png",
            "Assets/Art/UI/Source/icons_gold_sheet.png",
            "Assets/UI/Sprites/Buttons/highlight_hover.png",
            "Assets/UI/Sprites/Buttons/sort_square_sheet.png",
            "Assets/UI/Sprites/Glyphs/Suits/suits_all.png",
            "Assets/UI/Sprites/Glyphs/Ranks/ranks_1_4.png",
            "Assets/UI/Sprites/Glyphs/Ranks/ranks_5_8.png",
            "Assets/UI/Sprites/Glyphs/Ranks/ranks_9_10_J_Q.png",
            "Assets/UI/Sprites/Glyphs/Ranks/ranks_K_A_Joker_Plus.png",
        };

        foreach (var path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void BuildMainButton()
    {
        var normal = FindSprite("btn_main_sheet", "Main_Normal");
        var hover = FindSprite("btn_main_sheet", "Main_Hover");
        var pressed = FindSprite("btn_main_sheet", "Main_Pressed");
        var disabled = FindSprite("btn_main_sheet", "Main_Disabled");
        if (normal == null) return;

        var root = NewUiObject("Button_Principal", new Vector2(520f, 160f));
        var image = root.AddComponent<Image>();
        image.sprite = normal;
        image.type = Image.Type.Sliced;

        var button = root.AddComponent<Button>();
        button.transition = Button.Transition.SpriteSwap;
        var state = button.spriteState;
        state.highlightedSprite = hover;
        state.pressedSprite = pressed;
        state.disabledSprite = disabled;
        button.spriteState = state;
        button.targetGraphic = image;

        var scaleFx = root.AddComponent<UiButtonScaleFX>();
        scaleFx.hoverScale = 1.03f;
        scaleFx.pressScale = 0.98f;
        scaleFx.tweenDuration = 0.08f;

        AddLabel(root, "Jogar", 52f);

        SavePrefab(root, Path.Combine(LegacyPrefabPath, "Button_Principal.prefab"));
    }

    private static void BuildIconButton()
    {
        var normal = FindSprite("btn_icon_square_sheet", "IconSquare_Normal");
        var hover = FindSprite("btn_icon_square_sheet", "IconSquare_Hover");
        var pressed = FindSprite("btn_icon_square_sheet", "IconSquare_Pressed");
        var disabled = FindSprite("btn_icon_square_sheet", "IconSquare_Disabled");
        if (normal == null) return;

        var root = NewUiObject("Button_IconSquare", new Vector2(160f, 160f));
        var image = root.AddComponent<Image>();
        image.sprite = normal;
        image.type = Image.Type.Sliced;

        var button = root.AddComponent<Button>();
        button.transition = Button.Transition.SpriteSwap;
        var state = button.spriteState;
        state.highlightedSprite = hover;
        state.pressedSprite = pressed;
        state.disabledSprite = disabled;
        button.spriteState = state;
        button.targetGraphic = image;

        var scaleFx = root.AddComponent<UiButtonScaleFX>();
        scaleFx.hoverScale = 1.03f;
        scaleFx.pressScale = 0.98f;
        scaleFx.tweenDuration = 0.08f;

        var icon = FindSprite("icons_gold_sheet", "GoldIcon_0");
        if (icon != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(root.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(80f, 80f);
            iconRt.anchoredPosition = Vector2.zero;

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
        }

        SavePrefab(root, Path.Combine(LegacyPrefabPath, "Button_IconSquare.prefab"));
    }

    private static void BuildPillButton()
    {
        var pill = LoadSingleSprite("btn_pill");
        if (pill == null) return;

        var root = NewUiObject("Button_Pill", new Vector2(460f, 140f));
        var image = root.AddComponent<Image>();
        image.sprite = pill;
        image.type = Image.Type.Sliced;

        var button = root.AddComponent<Button>();
        button.transition = Button.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = image;

        var scaleFx = root.AddComponent<UiButtonScaleFX>();
        scaleFx.hoverScale = 1.03f;
        scaleFx.pressScale = 0.98f;
        scaleFx.tweenDuration = 0.08f;

        AddLabel(root, "Filtro", 44f);

        SavePrefab(root, Path.Combine(LegacyPrefabPath, "Button_Pill.prefab"));
    }

    private static void BuildFramePanel()
    {
        var frame = FindSprite("frame_panel", "FramePanel_0") ?? LoadSingleSprite("frame_panel");
        if (frame == null) return;

        var root = NewUiObject("Panel_Frame", new Vector2(800f, 520f));
        var image = root.AddComponent<Image>();
        image.sprite = frame;
        image.type = Image.Type.Sliced;

        SavePrefab(root, Path.Combine(LegacyPrefabPath, "Panel_Frame.prefab"));
    }

    private static void BuildHighlight()
    {
        var highlight = LoadSingleSprite("highlight_hover");
        if (highlight == null) return;

        var root = NewUiObject("Highlight_Hover", new Vector2(480f, 180f));
        var image = root.AddComponent<Image>();
        image.sprite = highlight;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        SavePrefab(root, Path.Combine(LegacyPrefabPath, "Highlight_Hover.prefab"));
    }

    private static void BuildCardGlyphButton()
    {
        var highlight = LoadSingleSprite("highlight_hover", "Buttons") ?? LoadSingleSprite("highlight_hover");
        if (highlight == null) return;

        var root = NewUiObject("UI_Button_CardGlyph", new Vector2(220f, 160f));
        var image = root.AddComponent<Image>();
        image.sprite = highlight;
        image.type = highlight.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;

        var button = root.AddComponent<Button>();
        button.transition = Button.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = image;

        var scaleFx = root.AddComponent<UiButtonScaleFX>();
        scaleFx.hoverScale = 1.03f;
        scaleFx.pressScale = 0.98f;
        scaleFx.tweenDuration = 0.08f;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        var glyphButton = root.AddComponent<CardGlyphButton>();
        glyphButton.SetIcon(FindSprite("suits_all", "Suit_0", "Glyphs/Suits"));
        glyphButton.ApplyIconSize();

        SavePrefab(root, Path.Combine(UiPrefabPath, "UI_Button_CardGlyph.prefab"));
    }

    private static void CreateCardGlyphExamples()
    {
        var prefabPath = CombineAssetPath(UiPrefabPath, null, "UI_Button_CardGlyph.prefab");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        var canvas = FindCanvas();
        if (canvas == null) return;

        var parent = canvas.transform.Find("CardGlyphExamples");
        if (parent == null)
        {
            var holder = new GameObject("CardGlyphExamples", typeof(RectTransform));
            holder.transform.SetParent(canvas.transform, false);
            parent = holder.transform;
        }

        var size = new Vector2(220f, 160f);
        float spacing = 16f;
        float rightPad = 24f;
        float bottomPad = 24f;

        var suitButton = GetOrCreateExample(parent, prefab, "CardGlyph_Suit");
        var rankButton = GetOrCreateExample(parent, prefab, "CardGlyph_Rank");

        PositionExample(suitButton, size, new Vector2(-rightPad, bottomPad));
        PositionExample(rankButton, size, new Vector2(-rightPad - size.x - spacing, bottomPad));

        var suitSprite = FindSprite("suits_all", "Suit_0", "Glyphs/Suits");
        var rankSprite = FindSprite("ranks_1_4", "Rank_0", "Glyphs/Ranks");

        ApplyGlyphSprite(suitButton, suitSprite);
        ApplyGlyphSprite(rankButton, rankSprite);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
#endif
    }

    private static void CleanupCardGlyphExamples()
    {
#if UNITY_EDITOR
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name != "CardGlyphExamples" && go.name != "CardGlyph_Suit" && go.name != "CardGlyph_Rank")
                continue;
            if (EditorUtility.IsPersistent(go)) continue;
            Object.DestroyImmediate(go);
        }
#endif
    }

    private static GameObject GetOrCreateExample(Transform parent, GameObject prefab, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab, parent);
        }
        instance.name = name;
        return instance;
    }

    private static void PositionExample(GameObject obj, Vector2 size, Vector2 anchoredPos)
    {
        if (obj == null) return;
        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one;
    }

    private static void ApplyGlyphSprite(GameObject obj, Sprite sprite)
    {
        if (obj == null || sprite == null) return;
        var glyph = obj.GetComponent<CardGlyphButton>();
        if (glyph != null)
        {
            glyph.SetIcon(sprite);
            glyph.ApplyIconSize();
            return;
        }

        var icon = obj.transform.Find("Icon");
        if (icon == null) return;
        var img = icon.GetComponent<Image>();
        if (img == null) return;
        img.sprite = sprite;
        img.preserveAspect = true;
    }

    private static GameObject NewUiObject(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        return go;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        var normalized = path.Replace("\\", "/");
        Directory.CreateDirectory(Path.GetDirectoryName(normalized) ?? LegacyPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, normalized);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Sprite[] LoadSprites(string baseName)
    {
        return LoadSprites(baseName, null);
    }

    private static Sprite FindSprite(string baseName, string spriteName)
    {
        return FindSprite(baseName, spriteName, null);
    }

    private static Sprite LoadSingleSprite(string baseName)
    {
        return LoadSingleSprite(baseName, null);
    }

    private static Sprite[] LoadSprites(string baseName, string subPath)
    {
        var assetPath = FindAssetPath(baseName, subPath);
        if (string.IsNullOrEmpty(assetPath))
            return System.Array.Empty<Sprite>();
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
    }

    private static Sprite FindSprite(string baseName, string spriteName, string subPath)
    {
        return LoadSprites(baseName, subPath).FirstOrDefault(s => s.name == spriteName);
    }

    private static Sprite LoadSingleSprite(string baseName, string subPath)
    {
        var assetPath = FindAssetPath(baseName, subPath);
        if (string.IsNullOrEmpty(assetPath))
            return null;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null) return sprite;

        // Multi-sprite fallback (pick first)
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
        if (sprites.Length > 0)
            return sprites[0];
        return null;
    }

    private static string FindAssetPath(string baseName, string subPath)
    {
        string fileName = $"{baseName}.png";
        foreach (var root in SpriteRoots)
        {
            var path = CombineAssetPath(root, subPath, fileName);
            if (AssetExists(path))
                return path;
        }
        return null;
    }

    private static string CombineAssetPath(string root, string subPath, string fileName)
    {
        if (string.IsNullOrEmpty(subPath))
            return $"{root}/{fileName}".Replace("\\", "/");
        return $"{root}/{subPath}/{fileName}".Replace("\\", "/");
    }

    private static bool AssetExists(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            return true;
        var fullPath = Path.GetFullPath(assetPath);
        return File.Exists(fullPath);
    }

    private static void AddLabel(GameObject root, string text, float maxFontSize)
    {
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(root.transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(48f, 18f);
        rt.offsetMax = new Vector2(-48f, -18f);

        // TMP obrigatorio para nitidez/escala
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 24f;
        tmp.fontSizeMax = maxFontSize;
        tmp.raycastTarget = false;
        tmp.color = new Color32(0xFF, 0xF3, 0xD4, 0xFF);

        ApplyGoldTextMaterial(tmp);

        var textFx = root.GetComponent<UiButtonTextFX>();
        if (textFx == null)
            textFx = root.AddComponent<UiButtonTextFX>();
        textFx.text = tmp;
    }

    private static void ApplyGoldTextMaterial(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        var shared = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial : tmp.fontMaterial;
        if (shared == null) return;

        var mat = new Material(shared);
        mat.name = "TMP_Gold_Normal_Instance";

        if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(0x3A, 0x22, 0x08, 0xFF));
        }

        if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.45f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 2f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -2f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.45f);
        }

        tmp.fontMaterial = mat;
    }

    private static Canvas FindCanvas()
    {
        Canvas[] canvases;
#if UNITY_2023_1_OR_NEWER
        canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
        canvases = Object.FindObjectsOfType<Canvas>();
#endif
        if (canvases != null && canvases.Length > 0)
        {
            Canvas best = canvases[0];
            foreach (var canvas in canvases)
            {
                if (canvas.sortingOrder > best.sortingOrder)
                    best = canvas;
            }
            return best;
        }

        var go = new GameObject("GameplayCanvas");
        var canvasComp = go.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvasComp;
    }
}
