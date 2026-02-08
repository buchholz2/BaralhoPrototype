using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UiButtonTextFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public TextMeshProUGUI text;
    public Color normalColor = new Color32(0xFF, 0xF3, 0xD4, 0xFF);
    public Color hoverColor = Color.white;
    public Color pressedColor = new Color32(0xF1, 0xD9, 0xA8, 0xFF);
    public Color disabledColor = new Color32(0xC9, 0xB3, 0x8C, 0xCC);

    public Vector2 pressedOffset = new Vector2(0f, -2f);
    [Range(0f, 0.5f)] public float hoverUnderlayBoost = 0.12f;

    private RectTransform _textRt;
    private Vector2 _basePos;
    private Button _button;
    private bool _hovering;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();
        _button = GetComponent<Button>();
        if (text != null)
        {
            _textRt = text.rectTransform;
            _basePos = _textRt.anchoredPosition;
            ApplyStateColor(normalColor);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        _hovering = true;
        // Hover: clareia e aumenta o underlay
        ApplyStateColor(hoverColor, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        _hovering = false;
        ResetPressOffset();
        ApplyStateColor(normalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        // Pressionado: escurece e desce 2px
        ApplyStateColor(pressedColor);
        ApplyPressOffset();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        ResetPressOffset();
        ApplyStateColor(_hovering ? hoverColor : normalColor, _hovering);
    }

    private bool CanAnimate()
    {
        if (_button == null) return true;
        if (!_button.interactable)
        {
            ApplyStateColor(disabledColor);
            return false;
        }
        return true;
    }

    private void ApplyPressOffset()
    {
        if (_textRt == null) return;
        _textRt.anchoredPosition = _basePos + pressedOffset;
    }

    private void ResetPressOffset()
    {
        if (_textRt == null) return;
        _textRt.anchoredPosition = _basePos;
    }

    private void ApplyStateColor(Color color, bool boostUnderlay = false)
    {
        if (text == null) return;
        text.color = color;

        var mat = text.fontMaterial;
        if (mat == null) return;
        if (!mat.HasProperty(ShaderUtilities.ID_UnderlayColor)) return;

        var underlay = mat.GetColor(ShaderUtilities.ID_UnderlayColor);
        float baseAlpha = 0.45f;
        float alpha = boostUnderlay ? Mathf.Clamp01(baseAlpha + hoverUnderlayBoost) : baseAlpha;
        underlay.a = alpha;
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, underlay);
        text.fontMaterial = mat;
    }
}
