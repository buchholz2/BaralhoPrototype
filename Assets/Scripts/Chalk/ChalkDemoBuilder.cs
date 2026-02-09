using TMPro;
using UnityEngine;

[ExecuteAlways]
public class ChalkDemoBuilder : MonoBehaviour
{
    [Header("Shared Assets")]
    public Material chalkOverlayMaterial;
    public Material chalkTextMaterial;
    public Texture2D grainTexture;

    [Header("Defaults")]
    public Color chalkTint = new Color(0.93f, 0.91f, 0.86f, 0.9f);
    [Range(0f, 1f)] public float opacity = 0.24f;
    [Range(0f, 1f)] public float grainStrength = 0.62f;
    public Vector2 grainScale = new Vector2(5f, 5f);
    public int baseSortingOrder = 20;

    [ContextMenu("Build Chalk Demo")]
    public void BuildDemo()
    {
        BuildRectZone("DrawPileZone", new Vector3(-3.1f, 1.2f, 0f), new Vector2(2.1f, 2.8f), "DRAW PILE", baseSortingOrder);
        BuildRectZone("DiscardZone", new Vector3(3.1f, 1.2f, 0f), new Vector2(2.1f, 2.8f), "DISCARD", baseSortingOrder + 1);
        BuildRectZone("PlayerAreaZone", new Vector3(0f, -2.8f, 0f), new Vector2(7.8f, 2.3f), "PLAYER AREA", baseSortingOrder + 2);
        BuildRectZone("OpponentAreaZone", new Vector3(0f, 2.95f, 0f), new Vector2(7.8f, 1.9f), "OPPONENT AREA", baseSortingOrder + 3);
    }

    private void BuildRectZone(string zoneName, Vector3 localPosition, Vector2 size, string label, int sortingOrder)
    {
        Transform zoneTransform = FindOrCreateChild(zoneName);
        zoneTransform.localPosition = localPosition;
        zoneTransform.localRotation = Quaternion.identity;
        zoneTransform.localScale = Vector3.one;

        ChalkLine line = zoneTransform.GetComponent<ChalkLine>();
        if (line == null) line = zoneTransform.gameObject.AddComponent<ChalkLine>();
        line.chalkMaterial = chalkOverlayMaterial;
        line.grainTexture = grainTexture;
        line.opacity = opacity;
        line.grainStrength = grainStrength;
        line.grainScale = grainScale;
        line.chalkTint = chalkTint;
        line.width = 0.16f;
        line.repeatsPerUnit = 2.8f;
        line.sortingLayerName = "ChalkOverlay";
        line.orderInLayer = sortingOrder;

        ChalkZone zone = zoneTransform.GetComponent<ChalkZone>();
        if (zone == null) zone = zoneTransform.gameObject.AddComponent<ChalkZone>();
        zone.zoneShape = ChalkZone.ZoneShape.RectZone;
        zone.rectSize = size;
        zone.thickness = 0.16f;
        zone.regenerateOnEnable = false;
        zone.Rebuild();

        BuildLabel(zoneTransform, label, size.y * 0.55f, sortingOrder + 2);
    }

    private void BuildLabel(Transform parent, string text, float yOffset, int sortingOrder)
    {
        Transform labelTransform = parent.Find("Label");
        if (labelTransform == null)
        {
            var labelGo = new GameObject("Label");
            labelTransform = labelGo.transform;
            labelTransform.SetParent(parent, false);
        }

        labelTransform.localPosition = new Vector3(0f, yOffset, 0f);
        labelTransform.localRotation = Quaternion.identity;

        TMP_Text tmp = labelTransform.GetComponent<TMP_Text>();
        if (tmp == null) tmp = labelTransform.gameObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 1.0f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        Renderer textRenderer = labelTransform.GetComponent<Renderer>();
        if (textRenderer == null)
            textRenderer = labelTransform.GetComponentInChildren<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = "ChalkOverlay";
            textRenderer.sortingOrder = sortingOrder;
        }

        ChalkText chalkText = labelTransform.GetComponent<ChalkText>();
        if (chalkText == null) chalkText = labelTransform.gameObject.AddComponent<ChalkText>();
        chalkText.targetText = tmp;
        chalkText.chalkTemplateMaterial = chalkTextMaterial;
        chalkText.grainTexture = grainTexture;
        chalkText.chalkTint = chalkTint;
        chalkText.opacity = opacity;
        chalkText.grainStrength = grainStrength;
        chalkText.grainScale = grainScale;
        chalkText.instantiatePerText = true;
        chalkText.ApplyChalk();
    }

    private Transform FindOrCreateChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child;

        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go.transform;
    }
}
