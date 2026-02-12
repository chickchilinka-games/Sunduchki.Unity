using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components
{
    [AddComponentMenu("UI/Themed Slider")]
    public class ThemedSlider : Slider
    {
        [SerializeField] private ThemedStateStyler _styler;
        private ThemedSelectionState _last;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying && _styler != null)
            {
                _styler.EditorCachePaletteIds();
                _styler.Apply(MapState(currentSelectionState), true);
            }
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            _styler?.Initialize(this, () => MapState(currentSelectionState));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _last = MapState(currentSelectionState);
            _styler?.Apply(_last, true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _last = MapState(state);
            _styler?.Apply(_last, instant);
        }

        private static ThemedSelectionState MapState(SelectionState s) => s switch
        {
            SelectionState.Disabled    => ThemedSelectionState.Disabled,
            SelectionState.Highlighted => ThemedSelectionState.Highlighted,
            SelectionState.Pressed     => ThemedSelectionState.Pressed,
            SelectionState.Selected    => ThemedSelectionState.Selected,
            _                          => ThemedSelectionState.Normal
        };
    }
}
