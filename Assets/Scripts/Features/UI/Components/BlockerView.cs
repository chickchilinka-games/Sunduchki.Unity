using DG.Tweening;
using UnityEngine;

namespace Features.UI.Components
{
    public class BlockerView: MonoBehaviour
    {
        [SerializeField] private RectTransform _loadingIcon;
        [SerializeField] private float _pulseDuration = 1f;
        [SerializeField] private float _pulseScale = 1.2f;

        private Tweener _pulseTween;

        private void OnEnable()
        {
            _pulseTween = _loadingIcon
                .DOScale(_pulseScale, _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            _pulseTween?.Kill();
            _loadingIcon.localScale = Vector3.one;
        }
    }
}
