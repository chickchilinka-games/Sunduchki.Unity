using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Features.UI.Components;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class AnimatedToggleGroup : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private RectTransform _highlight;
    [FormerlySerializedAs("_highlightStyler")] [SerializeField] private ThemedStateStyler _styler;
    [SerializeField] private float _moveDuration = 0.25f;
    [SerializeField] private Ease _moveEase = Ease.OutCubic;
    [SerializeField] private bool _animateSize = true;
    
    private ThemedToggle[] _toggles;
    private ThemedToggle _current;

    
#if UNITY_EDITOR
    protected void OnValidate()
    {
        if (!Application.isPlaying && _styler != null)
        {
            _styler.EditorCachePaletteIds();
        }
    }
#endif
    
    private async void Start()
    {
        try
        {
            _toggles = GetComponentsInChildren<ThemedToggle>(includeInactive: false);

            foreach (var t in _toggles)
            {
                t.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        MoveHighlight(t);
                });

                t.onStateChanged.AddListener(state =>
                {
                    if (t == _current)
                        _styler?.Apply(state, false);
                });
            }

            // Wait for layout to be calculated
            await UniTask.NextFrame();

            var first = _toggles.FirstOrDefault(t => t.isOn);
            if (first != null)
            {
                _current = first;
                var rect = (RectTransform)first.transform;
                _highlight.anchoredPosition = rect.anchoredPosition;
                _highlight.sizeDelta = rect.sizeDelta;
                _styler?.Apply(ThemedSelectionState.Selected, true);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void MoveHighlight(ThemedToggle target)
    {
        _current = target;
        var rect = target.transform as RectTransform;

        _highlight.DOKill();
        _highlight.DOAnchorPos(rect.anchoredPosition, _moveDuration).SetEase(_moveEase);

        if (_animateSize)
            _highlight.DOSizeDelta(rect.sizeDelta, _moveDuration).SetEase(_moveEase);
        
        _styler?.Apply(ThemedSelectionState.Normal, false);
    }
}
