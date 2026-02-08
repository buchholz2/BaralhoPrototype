using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CardGlyphButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [Range(0.4f, 0.9f)]
    [SerializeField] private float iconSizeRatio = 0.62f;
    [SerializeField] private bool preserveAspect = true;

    public void SetIcon(Sprite sprite)
    {
        if (icon == null)
            icon = FindIcon();
        if (icon == null) return;
        icon.sprite = sprite;
        icon.preserveAspect = preserveAspect;
    }

    public void ApplyIconSize()
    {
        if (icon == null)
            icon = FindIcon();
        if (icon == null) return;

        var parentRt = GetComponent<RectTransform>();
        var size = parentRt.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = parentRt.sizeDelta;

        float side = Mathf.Min(size.x, size.y) * iconSizeRatio;
        var iconRt = icon.rectTransform;
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(side, side);
        icon.preserveAspect = preserveAspect;
    }

    private void Reset()
    {
        icon = FindIcon();
        ApplyIconSize();
    }

    private void OnValidate()
    {
        ApplyIconSize();
    }

    private Image FindIcon()
    {
        var child = transform.Find("Icon");
        if (child == null) return null;
        return child.GetComponent<Image>();
    }
}
