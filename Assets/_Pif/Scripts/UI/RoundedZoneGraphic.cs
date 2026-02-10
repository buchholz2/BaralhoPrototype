using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pif.UI
{
    [AddComponentMenu("UI/Pif/Rounded Zone Graphic")]
    public class RoundedZoneGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 128f)] private float cornerRadius = 28f;
        [SerializeField, Range(0f, 24f)] private float strokeWidth = 1.5f;
        [SerializeField, Range(2, 24)] private int cornerSegments = 8;
        [SerializeField, Range(0f, 1f)] private float strokeOpacity = 0.35f;
        [SerializeField, Range(0f, 1f)] private float fillOpacity = 0.05f;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float StrokeWidth
        {
            get => strokeWidth;
            set
            {
                strokeWidth = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                cornerSegments = Mathf.Clamp(value, 2, 24);
                SetVerticesDirty();
            }
        }

        public float StrokeOpacity
        {
            get => strokeOpacity;
            set
            {
                strokeOpacity = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public float FillOpacity
        {
            get => fillOpacity;
            set
            {
                fillOpacity = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 1f || rect.height <= 1f)
                return;

            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float outerRadius = Mathf.Clamp(cornerRadius, 0f, maxRadius);
            int segments = Mathf.Clamp(cornerSegments, 2, 24);

            List<Vector2> outer = BuildRoundedRect(rect, outerRadius, segments);
            if (outer.Count < 3)
                return;

            Color32 strokeColor = color;
            strokeColor.a = (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01(strokeOpacity) * 255f);

            if (fillOpacity > 0f)
            {
                Color32 fillColor = color;
                fillColor.a = (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01(fillOpacity) * 255f);
                AddFill(vh, outer, fillColor);
            }

            if (strokeWidth <= 0.01f || strokeColor.a <= 0)
                return;

            Rect innerRect = new Rect(
                rect.xMin + strokeWidth,
                rect.yMin + strokeWidth,
                Mathf.Max(0f, rect.width - 2f * strokeWidth),
                Mathf.Max(0f, rect.height - 2f * strokeWidth));

            if (innerRect.width <= 0.01f || innerRect.height <= 0.01f)
                return;

            float innerRadius = Mathf.Clamp(outerRadius - strokeWidth, 0f, Mathf.Min(innerRect.width, innerRect.height) * 0.5f);
            List<Vector2> inner = BuildRoundedRect(innerRect, innerRadius, segments);
            if (inner.Count != outer.Count)
                return;

            AddStroke(vh, outer, inner, strokeColor);
        }

        private static List<Vector2> BuildRoundedRect(Rect rect, float radius, int segments)
        {
            var points = new List<Vector2>(segments * 4);
            float r = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);

            if (r <= 0.01f)
            {
                points.Add(new Vector2(rect.xMin, rect.yMin));
                points.Add(new Vector2(rect.xMax, rect.yMin));
                points.Add(new Vector2(rect.xMax, rect.yMax));
                points.Add(new Vector2(rect.xMin, rect.yMax));
                return points;
            }

            AddCornerArc(points, new Vector2(rect.xMin + r, rect.yMin + r), r, 180f, 270f, segments);
            AddCornerArc(points, new Vector2(rect.xMax - r, rect.yMin + r), r, 270f, 360f, segments);
            AddCornerArc(points, new Vector2(rect.xMax - r, rect.yMax - r), r, 0f, 90f, segments);
            AddCornerArc(points, new Vector2(rect.xMin + r, rect.yMax - r), r, 90f, 180f, segments);

            return points;
        }

        private static void AddCornerArc(List<Vector2> points, Vector2 center, float radius, float startDeg, float endDeg, int segments)
        {
            float step = (endDeg - startDeg) / segments;
            for (int i = 0; i < segments; i++)
            {
                float angleDeg = startDeg + step * i;
                float angle = angleDeg * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static void AddFill(VertexHelper vh, IReadOnlyList<Vector2> polygon, Color32 color)
        {
            int start = vh.currentVertCount;
            Vector2 center = Vector2.zero;
            for (int i = 0; i < polygon.Count; i++)
                center += polygon[i];
            center /= polygon.Count;

            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < polygon.Count; i++)
                vh.AddVert(polygon[i], color, Vector2.zero);

            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                vh.AddTriangle(start, start + i + 1, start + next + 1);
            }
        }

        private static void AddStroke(VertexHelper vh, IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, Color32 color)
        {
            int start = vh.currentVertCount;
            int count = outer.Count;

            for (int i = 0; i < count; i++)
            {
                vh.AddVert(outer[i], color, Vector2.zero);
                vh.AddVert(inner[i], color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int outerA = start + i * 2;
                int innerA = outerA + 1;
                int outerB = start + next * 2;
                int innerB = outerB + 1;

                vh.AddTriangle(outerA, outerB, innerB);
                vh.AddTriangle(outerA, innerB, innerA);
            }
        }
    }
}
