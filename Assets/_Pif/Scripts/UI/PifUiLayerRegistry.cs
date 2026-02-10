using UnityEngine;

namespace Pif.UI
{
    public static class PifUiLayerRegistry
    {
        public static RectTransform HoverLayer { get; private set; }
        public static RectTransform DragLayer { get; private set; }

        public static bool HasLayering => HoverLayer != null || DragLayer != null;

        public static void Register(RectTransform hoverLayer, RectTransform dragLayer)
        {
            HoverLayer = hoverLayer;
            DragLayer = dragLayer;
        }

        public static void Clear(RectTransform ownerHoverLayer = null, RectTransform ownerDragLayer = null)
        {
            if (ownerHoverLayer != null && HoverLayer != ownerHoverLayer)
                return;
            if (ownerDragLayer != null && DragLayer != ownerDragLayer)
                return;

            HoverLayer = null;
            DragLayer = null;
        }

        public static bool TryGetLayers(out RectTransform hoverLayer, out RectTransform dragLayer)
        {
            hoverLayer = HoverLayer;
            dragLayer = DragLayer;
            return hoverLayer != null || dragLayer != null;
        }
    }
}
