using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components.TabButtons
{
    public class TabButton : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private float _backgroundFadeDuration = 0.5f;
        [SerializeField] private CanvasGroup _selectionBackground;
        [SerializeField] private Button _button;

        public string Id => _id;
        public Observable<Unit> OnClick => _clickCommand;
        private readonly ReactiveCommand _clickCommand = new();
        private Tweener _tween;
        private void Awake()
        {
            _button.onClick.AsObservable()
                .Subscribe(_ => _clickCommand.Execute(Unit.Default))
                .AddTo(this);
        }

        public void Select(bool immediately = false)
        {
            if (!immediately)
                _tween = _selectionBackground.DOFade(1f, _backgroundFadeDuration);
            else
                _selectionBackground.alpha = 1;
        }

        public void Deselect(bool immediately = false)
        {
            if (!immediately)
                _tween = _selectionBackground.DOFade(0f, _backgroundFadeDuration);
            else
                _selectionBackground.alpha = 0;
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
