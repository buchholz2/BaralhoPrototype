using UnityEngine;
using UnityEngine.EventSystems;

public class WorldPileClick : MonoBehaviour, IPointerClickHandler
{
    private GameBootstrap _controller;
    private bool _usePointerEvents;

    public void Init(GameBootstrap controller)
    {
        _controller = controller;
    }

    private void Awake()
    {
        var cam = Camera.main;
        _usePointerEvents = EventSystem.current != null && cam != null && cam.GetComponent<PhysicsRaycaster>() != null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        if (_controller == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _controller.DrawFromPile();
    }

    private void OnMouseUp()
    {
        if (_usePointerEvents) return;
        if (_controller == null) return;
        _controller.DrawFromPile();
    }
}
