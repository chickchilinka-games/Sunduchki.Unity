using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;
using uPalette.Runtime.Core;

namespace Features.UI.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CarouselGraphicElement : MonoBehaviour
    {
        [SerializeField]
        private Graphic _graphic;
        [Header("uPalette Colors")] 
        [SerializeField]
        private ColorEntryId _selectedColorId;

        [SerializeField] private ColorEntryId _deselectedColorId;

        [Header("Falloff")]
        [SerializeField]
        private AnimationCurve _falloff = null;
        
        [SerializeField, Min(0.01f)]
        private float _normalizeByViewportFactor = 0.5f;

        [SerializeField]
        private bool _lerpAlpha = true;
        
        private RectTransform _rt;
        private Color _colSelected;
        private Color _colDeselected;
        private CarouselBase _carousel;
        

        private void Awake()
        {
            _rt ??= GetComponent<RectTransform>();
            _graphic = GetComponent<Graphic>();
            if (_carousel == null) _carousel = GetComponentInParent<CarouselBase>();

            if (!_carousel) return;

            // Subscribe to theme changes
            PaletteStore.Instance.ColorPalette.ActiveTheme
                .ToObservable()
                .Subscribe(_ =>
                {
                    CacheColors();
                    UpdateColorInstant();
                })
                .AddTo(this);

            _carousel.Scroll.OnValueChangedAsObservable()
                .Subscribe(_ => UpdateColorInstant())
                .AddTo(this);

            _carousel.OnIndexChanged += _ => UpdateColorInstant();
        }

        private void OnEnable()
        {
            CacheColors();
            UpdateColorInstant();
        }

        private void CacheColors()
        {
            var palette = PaletteStore.Instance.ColorPalette;

            _colSelected = palette.TryGetActiveValue(_selectedColorId.Value, out var selectedEntry)
                ? selectedEntry.Value
                : Color.white;

            _colDeselected = palette.TryGetActiveValue(_deselectedColorId.Value, out var deselectedEntry)
                ? deselectedEntry.Value
                : Color.gray;
        }

        private void UpdateColorInstant()
        {
            if (_carousel == null || _graphic == null) return;

            var axisSize = _carousel.IsVertical
                ? _carousel.Scroll.viewport.rect.height
                : _carousel.Scroll.viewport.rect.width;

            var normDivisor = Mathf.Max(1e-3f, axisSize * _normalizeByViewportFactor);

            // Use new unified method for correct distance calculation
            var dist = _carousel.GetDistanceFromViewportCenter(_rt);
            var t = Mathf.Clamp01(dist / normDivisor);

            var w = _falloff != null ? Mathf.Clamp01(_falloff.Evaluate(t)) : (1f - t);

            var from = _colDeselected;
            var to = _colSelected;
            if (!_lerpAlpha)
            {
                to.a = from.a;
            }

            var col = Color.Lerp(from, to, w);

            _graphic.color = col;
        }
    }
}
