using System.Collections;
using Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Components
{
    [DisallowMultipleComponent]
    internal class TabSandwich : MonoBehaviour, IResettableView
    {
        [Header("Target")]
        [SerializeField] private LayoutElement targetLayout;
        [SerializeField] private Image toggleImage;
        [SerializeField] private Button toggleButton;

        [Header("Dimensions")]
        [SerializeField] private float foldedWidth = 280f;
        [SerializeField] private float unfoldedWidth = 640f;

        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.25f;
        [SerializeField] private float foldedAngle = 0f;
        [SerializeField] private float unfoldedAngle = 90f;
        [SerializeField] private bool startUnfolded;

        private Coroutine _animation;
        private bool _isUnfolded;

        private void Awake()
        {
            if (toggleButton == null)
            {
                toggleButton = GetComponent<Button>();
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }
            else
            {
                Debug.LogWarning("[TabSandwich] Toggle button is not set. Toggle() must be called manually.");
            }
        }

        private void OnEnable()
        {
            ResetView();
        }

        private void OnDisable()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }
        }

        public void Toggle()
        {
            if (_isUnfolded)
            {
                Fold();
            }
            else
            {
                Unfold();
            }
        }

        public void Fold()
        {
            AnimateTo(false, false);
        }

        public void Unfold()
        {
            AnimateTo(true, false);
        }

        public void FoldInstant()
        {
            AnimateTo(false, true);
        }

        public void UnfoldInstant()
        {
            AnimateTo(true, true);
        }

        public void ResetView()
        {
            if (startUnfolded)
            {
                UnfoldInstant();
            }
            else
            {
                FoldInstant();
            }
        }

        private void AnimateTo(bool targetState, bool instant)
        {
            if (targetLayout == null)
            {
                Debug.LogWarning("[TabSandwich] Target layout element is missing.");
                return;
            }

            if (instant || animationDuration <= 0f)
            {
                ApplyState(targetState);
                return;
            }

            if (_animation != null)
            {
                StopCoroutine(_animation);
            }

            _animation = StartCoroutine(AnimateTween(targetState));
        }

        private IEnumerator AnimateTween(bool targetState)
        {
            float startWidth = targetLayout.preferredWidth;
            float endWidth = targetState ? unfoldedWidth : foldedWidth;

            float startAngle = GetCurrentAngle();
            float endAngle = targetState ? unfoldedAngle : foldedAngle;

            float duration = Mathf.Max(0.0001f, animationDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutQuad(t);

                float width = Mathf.Lerp(startWidth, endWidth, eased);
                targetLayout.preferredWidth = width;

                if (toggleImage != null)
                {
                    float angle = Mathf.LerpAngle(startAngle, endAngle, eased);
                    SetAngle(angle);
                }

                yield return null;
            }

            ApplyState(targetState);
            _animation = null;
        }

        private void ApplyState(bool targetState)
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            float targetWidth = targetState ? unfoldedWidth : foldedWidth;
            targetLayout.preferredWidth = targetWidth;

            if (toggleImage != null)
            {
                float angle = targetState ? unfoldedAngle : foldedAngle;
                SetAngle(angle);
            }

            _isUnfolded = targetState;
        }

        private float GetCurrentAngle()
        {
            if (toggleImage == null)
            {
                return 0f;
            }

            return toggleImage.rectTransform.localEulerAngles.z;
        }

        private void SetAngle(float angle)
        {
            if (toggleImage == null)
            {
                return;
            }

            var rect = toggleImage.rectTransform;
            var euler = rect.localEulerAngles;
            euler.z = angle;
            rect.localEulerAngles = euler;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}
