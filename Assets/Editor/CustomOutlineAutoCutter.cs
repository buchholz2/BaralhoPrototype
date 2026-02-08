using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

internal static class CustomOutlineAutoCutter
{
    private const float MinComponentRatio = 0.08f;
    private const int MinComponentPixels = 10;

    private struct ComponentBounds
    {
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public int area;
    }

    /// <summary>
    /// Calcula um retangulo de corte usando contorno alpha (estilo Custom Outline).
    /// </summary>
    public static bool TryComputeCutRect(
        Color32[] pixels,
        int texWidth,
        int texHeight,
        RectInt cellRect,
        byte alphaThreshold,
        int padding,
        bool verticalOnly,
        int minHeight,
        bool forceSquare,
        bool preferLargestComponent,
        out RectInt cutRect)
    {
        cutRect = cellRect;
        if (pixels == null || pixels.Length == 0)
            return false;
        if (texWidth <= 0 || texHeight <= 0)
            return false;
        if (cellRect.width <= 0 || cellRect.height <= 0)
            return false;

        int rectMinX = Mathf.Clamp(cellRect.xMin, 0, texWidth - 1);
        int rectMaxX = Mathf.Clamp(cellRect.xMax, 1, texWidth);
        int rectMinY = Mathf.Clamp(cellRect.yMin, 0, texHeight - 1);
        int rectMaxY = Mathf.Clamp(cellRect.yMax, 1, texHeight);

        int w = rectMaxX - rectMinX;
        int h = rectMaxY - rectMinY;
        if (w <= 0 || h <= 0)
            return false;

        var visited = new bool[w * h];
        var components = new List<ComponentBounds>(8);
        var queue = new Queue<int>(256);

        for (int y = rectMinY; y < rectMaxY; y++)
        {
            int ly = y - rectMinY;
            for (int x = rectMinX; x < rectMaxX; x++)
            {
                int lx = x - rectMinX;
                int localIndex = ly * w + lx;
                if (visited[localIndex])
                    continue;

                int textureIndex = y * texWidth + x;
                if (pixels[textureIndex].a <= alphaThreshold)
                    continue;

                visited[localIndex] = true;
                queue.Enqueue(localIndex);

                var comp = new ComponentBounds
                {
                    minX = x,
                    minY = y,
                    maxX = x,
                    maxY = y,
                    area = 0
                };

                while (queue.Count > 0)
                {
                    int p = queue.Dequeue();
                    int px = p % w;
                    int py = p / w;
                    int worldX = rectMinX + px;
                    int worldY = rectMinY + py;
                    comp.area++;

                    if (worldX < comp.minX) comp.minX = worldX;
                    if (worldY < comp.minY) comp.minY = worldY;
                    if (worldX > comp.maxX) comp.maxX = worldX;
                    if (worldY > comp.maxY) comp.maxY = worldY;

                    EnqueueIfOpaque(worldX - 1, worldY);
                    EnqueueIfOpaque(worldX + 1, worldY);
                    EnqueueIfOpaque(worldX, worldY - 1);
                    EnqueueIfOpaque(worldX, worldY + 1);
                }

                components.Add(comp);
            }
        }

        if (components.Count == 0)
        {
            cutRect = new RectInt(rectMinX, rectMinY, w, h);
            return true;
        }

        int largestArea = 0;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i].area > largestArea)
                largestArea = components[i].area;
        }

        int minX = rectMaxX - 1;
        int minY = rectMaxY - 1;
        int maxX = rectMinX;
        int maxY = rectMinY;

        if (preferLargestComponent)
        {
            var largest = components[0];
            for (int i = 1; i < components.Count; i++)
            {
                if (components[i].area > largest.area)
                    largest = components[i];
            }

            minX = largest.minX;
            minY = largest.minY;
            maxX = largest.maxX;
            maxY = largest.maxY;
        }
        else
        {
            int keepThreshold = Mathf.Max(MinComponentPixels, Mathf.RoundToInt(largestArea * MinComponentRatio));
            bool anyKept = false;
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.area < keepThreshold)
                    continue;

                anyKept = true;
                if (c.minX < minX) minX = c.minX;
                if (c.minY < minY) minY = c.minY;
                if (c.maxX > maxX) maxX = c.maxX;
                if (c.maxY > maxY) maxY = c.maxY;
            }

            if (!anyKept)
            {
                var largest = components[0];
                for (int i = 1; i < components.Count; i++)
                {
                    if (components[i].area > largest.area)
                        largest = components[i];
                }

                minX = largest.minX;
                minY = largest.minY;
                maxX = largest.maxX;
                maxY = largest.maxY;
            }
        }

        int padMinX = Mathf.Max(rectMinX, minX - padding);
        int padMinY = Mathf.Max(rectMinY, minY - padding);
        int padMaxX = Mathf.Min(rectMaxX - 1, maxX + padding);
        int padMaxY = Mathf.Min(rectMaxY - 1, maxY + padding);

        int outX = padMinX;
        int outY = padMinY;
        int outW = Mathf.Max(1, (padMaxX - padMinX) + 1);
        int outH = Mathf.Max(1, (padMaxY - padMinY) + 1);

        if (verticalOnly)
        {
            outX = rectMinX;
            outW = w;
        }

        if (minHeight > 0 && outH < minHeight)
        {
            float center = outY + outH * 0.5f;
            int desired = Mathf.Min(minHeight, h);
            int newY = Mathf.RoundToInt(center - desired * 0.5f);
            newY = Mathf.Clamp(newY, rectMinY, rectMaxY - desired);
            outY = newY;
            outH = desired;
        }

        if (forceSquare)
            MakeSquareInsideCell(rectMinX, rectMinY, w, h, ref outX, ref outY, ref outW, ref outH);

        cutRect = new RectInt(outX, outY, outW, outH);
        return true;

        void EnqueueIfOpaque(int worldX, int worldY)
        {
            if (worldX < rectMinX || worldX >= rectMaxX || worldY < rectMinY || worldY >= rectMaxY)
                return;

            int lx = worldX - rectMinX;
            int ly = worldY - rectMinY;
            int li = ly * w + lx;
            if (visited[li])
                return;

            int ti = worldY * texWidth + worldX;
            if (pixels[ti].a <= alphaThreshold)
                return;

            visited[li] = true;
            queue.Enqueue(li);
        }
    }

    private static void MakeSquareInsideCell(int baseX, int baseY, int baseW, int baseH, ref int rectX, ref int rectY, ref int rectW, ref int rectH)
    {
        int side = Mathf.Max(rectW, rectH);
        side = Mathf.Min(side, Mathf.Min(baseW, baseH));
        side = Mathf.Max(1, side);

        float centerX = rectX + rectW * 0.5f;
        float centerY = rectY + rectH * 0.5f;
        int squareX = Mathf.RoundToInt(centerX - side * 0.5f);
        int squareY = Mathf.RoundToInt(centerY - side * 0.5f);

        squareX = Mathf.Clamp(squareX, baseX, baseX + baseW - side);
        squareY = Mathf.Clamp(squareY, baseY, baseY + baseH - side);

        rectX = squareX;
        rectY = squareY;
        rectW = side;
        rectH = side;
    }

#if UNITY_EDITOR
    private static readonly string[] ReimportFolders =
    {
        "Assets/Art/UI/Source",
        "Assets/UI/Sprites/Buttons",
        "Assets/UI/Sprites/Glyphs"
    };

    [MenuItem("Tools/UI/Rebuild Custom Outline Cuts")]
    private static void RebuildCustomOutlineCuts()
    {
        int processed = 0;
        foreach (var folder in ReimportFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                processed++;
            }
        }

        Debug.Log($"CustomOutlineAutoCutter: cortes reconstruidos em {processed} textura(s).");
    }
#endif
}
