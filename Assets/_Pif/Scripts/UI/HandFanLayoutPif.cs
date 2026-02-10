using UnityEngine;

namespace Pif.UI
{
    [DisallowMultipleComponent]
    public class HandFanLayoutPif : MonoBehaviour
    {
        [SerializeField] private HandFanLayout target;

        [Header("PIF Preset")]
        [SerializeField, Min(100f)] private float radius = 520f;
        [SerializeField, Range(0f, 90f)] private float maxAngle = 54f;
        [SerializeField] private float baseY = -210f;
        [SerializeField] private float spacing = 232f;
        [SerializeField, Range(0f, 0.95f)] private float overlap = 0.79f;
        [SerializeField] private bool rotateCards = true;

        [Header("Smoothing")]
        [SerializeField] private bool smooth = true;
        [SerializeField, Range(0.02f, 0.4f)] private float smoothTime = 0.08f;
        [SerializeField, Range(0.02f, 0.4f)] private float rotationSmoothTime = 0.08f;

        private void Reset()
        {
            if (target == null)
                target = GetComponent<HandFanLayout>();
        }

        private void Awake()
        {
            ApplyPreset();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            ApplyPreset();
        }

        public void ApplyPreset()
        {
            if (target == null)
                target = GetComponent<HandFanLayout>();

            if (target == null)
                return;

            target.radius = radius;
            target.maxAngle = maxAngle;
            target.baseY = baseY;
            target.spacing = spacing;
            target.overlap = overlap;
            target.rotateCards = rotateCards;
            target.smooth = smooth;
            target.smoothTime = smoothTime;
            target.rotationSmoothTime = rotationSmoothTime;
        }
    }
}
