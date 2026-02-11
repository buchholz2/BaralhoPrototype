using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PIF.UI
{
    /// <summary>
    /// Meld area for a player. Almost invisible when empty.
    /// </summary>
    public class PifMeldArea : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private HorizontalOrVerticalLayoutGroup layoutGroup;

        [Header("Settings")]
        [SerializeField] private float emptyAlpha = 0.02f;
        [SerializeField] private float filledAlpha = 0.15f;
        [SerializeField] private float cardScale = 0.75f;

        private readonly List<GameObject> _meldCardObjects = new List<GameObject>();
        private bool _hasContent = false;

        private void Start()
        {
            SetEmpty();
        }

        public void Bind(Image background, CanvasGroup group, HorizontalOrVerticalLayoutGroup layout)
        {
            backgroundImage = background;
            canvasGroup = group;
            layoutGroup = layout;
            UpdateVisibility();
        }

        public void AddMeld(List<Card> cards, GameObject cardPrefab)
        {
            if (cards == null || cards.Count == 0)
                return;

            GameObject meldGroup = new GameObject("MeldGroup");
            meldGroup.transform.SetParent(transform, false);

            HorizontalLayoutGroup hlg = meldGroup.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = -50f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            foreach (Card card in cards)
            {
                GameObject cardObj = cardPrefab != null
                    ? Instantiate(cardPrefab, meldGroup.transform)
                    : CreateFallbackCard(meldGroup.transform);

                cardObj.transform.localScale = Vector3.one * cardScale;

                _meldCardObjects.Add(cardObj);
            }

            _hasContent = true;
            UpdateVisibility();
        }

        public void ClearMelds()
        {
            foreach (GameObject obj in _meldCardObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            _meldCardObjects.Clear();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            _hasContent = false;
            UpdateVisibility();
        }

        private void SetEmpty()
        {
            _hasContent = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = _hasContent ? filledAlpha : emptyAlpha;
            }

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = _hasContent ? filledAlpha : emptyAlpha;
                backgroundImage.color = c;
            }
        }

        private GameObject CreateFallbackCard(Transform parent)
        {
            GameObject go = new GameObject("Card");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(90f, 120f);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.95f);
            img.raycastTarget = false;
            return go;
        }
    }
}
