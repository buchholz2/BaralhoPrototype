using UnityEngine;
using UnityEngine.EventSystems;

namespace Pif.UI
{
    public enum PifDropZoneType
    {
        None,
        Hand,
        DrawPile,
        DiscardPile,
        Vira,
        MeldCenter,
        MeldNorth,
        MeldWest,
        MeldEast
    }

    [DisallowMultipleComponent]
    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private PifDropZoneType zoneType = PifDropZoneType.None;
        [SerializeField, Range(0f, 1f)] private float hoverBoost = 0.08f;

        private RoundedZoneGraphic _zoneGraphic;
        private float _baseStrokeOpacity;

        public PifDropZoneType ZoneType => zoneType;

        public System.Action<DropZone, PointerEventData> CardDropped;

        public void SetZoneType(PifDropZoneType value)
        {
            zoneType = value;
        }

        private void Awake()
        {
            _zoneGraphic = GetComponent<RoundedZoneGraphic>();
            if (_zoneGraphic != null)
                _baseStrokeOpacity = _zoneGraphic.StrokeOpacity;
        }

        public void OnDrop(PointerEventData eventData)
        {
            CardDropped?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_zoneGraphic == null)
                return;

            _zoneGraphic.StrokeOpacity = Mathf.Clamp01(_baseStrokeOpacity + hoverBoost);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_zoneGraphic == null)
                return;

            _zoneGraphic.StrokeOpacity = _baseStrokeOpacity;
        }
    }
}
