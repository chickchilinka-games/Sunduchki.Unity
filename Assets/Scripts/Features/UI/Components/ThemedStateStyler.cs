using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using uPalette.Runtime.Core;
using R3;

namespace Features.UI.Components
{
    public enum ThemedSelectionState
    {
        Normal,
        Highlighted,
        Pressed,
        Selected,
        Disabled
    }

    [Serializable]
    public sealed class ThemedStateStyler
    {
        [Serializable]
        public class GraphicTheme
        {
            public Graphic Target;

            [Tooltip("Base uPalette path, e.g. 'Button/Primary/Text'")]
            public string GroupPath;

            [HideInInspector] public ColorEntryId NormalId;
            [HideInInspector] public ColorEntryId HighlightedId;
            [HideInInspector] public ColorEntryId PressedId;
            [HideInInspector] public ColorEntryId SelectedId;
            [HideInInspector] public ColorEntryId DisabledId;
        }

        [Header("Graphics")] [SerializeField] private GraphicTheme[] _graphics = Array.Empty<GraphicTheme>();

        [Header("Animation")] [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private Func<ThemedSelectionState> _getCurrentState;

        public void Initialize(MonoBehaviour owner, Func<ThemedSelectionState> getCurrentState)
        {
            _getCurrentState = getCurrentState;

            PaletteStore.Instance.ColorPalette.ActiveTheme
                .ToObservable()
                .Subscribe(_ =>
                {
                    var s = _getCurrentState != null ? _getCurrentState() : ThemedSelectionState.Normal;
                    Apply(s, true);
                })
                .AddTo(owner);
        }

#if UNITY_EDITOR
        public void EditorCachePaletteIds()
        {
            var palette = PaletteStore.Instance.ColorPalette;
            if (palette == null || palette.Entries == null)
                return;

            foreach (var g in _graphics)
            {
                if (string.IsNullOrEmpty(g.GroupPath))
                    continue;

                g.NormalId = FindId($"{g.GroupPath}/Normal", null);
                g.HighlightedId = FindId($"{g.GroupPath}/Highlighted", g.NormalId);
                g.PressedId = FindId($"{g.GroupPath}/Pressed", g.NormalId);
                g.SelectedId = FindId($"{g.GroupPath}/Selected", g.NormalId);
                g.DisabledId = FindId($"{g.GroupPath}/Disabled", g.NormalId);
            }

            ColorEntryId FindId(string name, ColorEntryId defaultEntry)
            {
                var entry = palette.Entries.Values.FirstOrDefault(e => e.Name.Value == name);
                return entry != null ? new ColorEntryId() { Value = entry.Id } : defaultEntry;
            }
        }
#endif

        public void Apply(ThemedSelectionState state, bool instant)
        {
            if (_graphics == null || _graphics.Length == 0)
                return;

            var palette = PaletteStore.Instance.ColorPalette;

            foreach (var g in _graphics)
            {
                if (g.Target == null)
                    continue;

                var id = state switch
                {
                    ThemedSelectionState.Highlighted => g.HighlightedId,
                    ThemedSelectionState.Pressed => g.PressedId,
                    ThemedSelectionState.Selected => g.SelectedId,
                    ThemedSelectionState.Disabled => g.DisabledId,
                    _ => g.NormalId
                };

                if (id == null || !palette.TryGetActiveValue(id.Value, out var colorEntry))
                    continue;

                var color = colorEntry.Value;
                if (instant || _fadeDuration <= 0f)
                    g.Target.color = color;
                else
                    g.Target.DOColor(color, _fadeDuration)
                        .SetEase(_ease)
                        .SetTarget(g.Target);
            }
        }
    }
}