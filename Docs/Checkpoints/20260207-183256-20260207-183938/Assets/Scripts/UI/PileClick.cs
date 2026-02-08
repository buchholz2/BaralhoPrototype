using UnityEngine;
using UnityEngine.EventSystems;

public class PileClick : MonoBehaviour, IPointerClickHandler
{
    private GameBootstrap _controller;
    private bool _isDrawPile;

    public void Init(GameBootstrap controller, bool isDrawPile)
    {
        _controller = controller;
        _isDrawPile = isDrawPile;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_controller == null) return;
        if (_isDrawPile)
            _controller.DrawFromPile();
    }
}
