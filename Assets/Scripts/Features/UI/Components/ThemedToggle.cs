using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Features.UI.Components
{
    [AddComponentMenu("UI/Themed Toggle")]
    public class ThemedToggle : Toggle
    {
        [FormerlySerializedAs("onClicked")] public UnityEvent<ThemedSelectionState> onStateChanged = new UnityEvent<ThemedSelectionState>();
        [Header("Styling")] 
        [SerializeField] private ThemedStateStyler _stylerOn;
        [SerializeField] private ThemedStateStyler _stylerOff;

        [Header("Extra graphics (optional)")] [SerializeField]
        private Graphic[] _graphicsWhenOn;

        [SerializeField] private Graphic[] _graphicsWhenOff;

        private ThemedSelectionState _last;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) 
                return;
            _stylerOn?.EditorCachePaletteIds();
            _stylerOff?.EditorCachePaletteIds();

            var state = MapState(currentSelectionState);
            if (isOn)
                _stylerOn?.Apply(state, true);
            else
                _stylerOff?.Apply(state, true);
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            _stylerOn?.Initialize(this, () => MapState(currentSelectionState));
            _stylerOff?.Initialize(this, () => MapState(currentSelectionState));

            onValueChanged.AddListener(_ => ApplyVisualState(false));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _last = MapState(currentSelectionState);
            ApplyVisualState(true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _last = MapState(state);
            onStateChanged.Invoke(_last);
            ApplyVisualState(instant);
        }

        private void ApplyVisualState(bool instant)
        {
            if (isOn)
                _stylerOn?.Apply(_last, instant);
            else
                _stylerOff?.Apply(_last, instant);

            var duration = (toggleTransition == ToggleTransition.Fade && !instant) ? 0.1f : 0f;

            SetGraphicsAlpha(_graphicsWhenOn, isOn ? 1f : 0f, duration);
            SetGraphicsAlpha(_graphicsWhenOff, isOn ? 0f : 1f, duration);
        }

        private void SetGraphicsAlpha(Graphic[] graphics, float alpha, float duration)
        {
            if (graphics == null) return;

            foreach (var g in graphics)
            {
                if (g == null) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    g.canvasRenderer?.SetAlpha(alpha);
                else
#endif
                    g.CrossFadeAlpha(alpha, duration, true);
            }
        }

        private static ThemedSelectionState MapState(SelectionState s) => s switch
        {
            SelectionState.Disabled => ThemedSelectionState.Disabled,
            SelectionState.Highlighted => ThemedSelectionState.Highlighted,
            SelectionState.Pressed => ThemedSelectionState.Pressed,
            SelectionState.Selected => ThemedSelectionState.Selected,
            _ => ThemedSelectionState.Normal
        };
    }
}
