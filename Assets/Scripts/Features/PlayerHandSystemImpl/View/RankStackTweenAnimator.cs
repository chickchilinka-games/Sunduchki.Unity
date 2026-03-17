using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.PlayerHandSystemImpl.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Features.PlayerHandSystemImpl.View
{
    internal readonly struct RankStackReceiveTweenRequest
    {
        public Image Card { get; }
        public RectTransform ReceiveOrigin { get; }
        public Vector2 BaseAnchoredPosition { get; }
        public Vector2 ReceiveOffset { get; }
        public Vector3 TargetWorldPosition { get; }
        public float Duration { get; }
        public float TimeoutBuffer { get; }

        public RankStackReceiveTweenRequest(
            Image card,
            RectTransform receiveOrigin,
            Vector2 baseAnchoredPosition,
            Vector2 receiveOffset,
            Vector3 targetWorldPosition,
            float duration,
            float timeoutBuffer)
        {
            Card = card;
            ReceiveOrigin = receiveOrigin;
            BaseAnchoredPosition = baseAnchoredPosition;
            ReceiveOffset = receiveOffset;
            TargetWorldPosition = targetWorldPosition;
            Duration = duration;
            TimeoutBuffer = timeoutBuffer;
        }
    }

    internal sealed class RankStackTweenAnimator
    {
        public async UniTask PlaySetCompleteAsync(
            CanvasGroup canvasGroup,
            float flashDuration,
            float fadeDuration,
            float timeoutBuffer,
            CancellationToken cancellationToken)
        {
            if (canvasGroup == null)
            {
                return;
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(canvasGroup.DOFade(0.3f, flashDuration));
            sequence.Append(canvasGroup.DOFade(1f, flashDuration));
            sequence.Append(canvasGroup.DOFade(0f, fadeDuration));
            await TweenAwaiter.AwaitAsync(
                sequence,
                (flashDuration * 2f) + fadeDuration + timeoutBuffer,
                cancellationToken);
        }

        public async UniTask PlayReceiveAsync(RankStackReceiveTweenRequest request, CancellationToken cancellationToken)
        {
            var card = request.Card;
            if (card == null)
            {
                return;
            }

            var rect = card.rectTransform;
            DOTween.Kill(card);
            DOTween.Kill(rect);
            SetImageAlpha(card, 0f);

            if (request.ReceiveOrigin != null)
            {
                rect.position = request.ReceiveOrigin.position;
            }
            else
            {
                rect.anchoredPosition = request.BaseAnchoredPosition + request.ReceiveOffset;
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            if (request.ReceiveOrigin != null)
            {
                sequence.Append(rect.DOMove(request.TargetWorldPosition, request.Duration).SetEase(Ease.OutQuad));
            }
            else
            {
                sequence.Append(rect.DOAnchorPos(request.BaseAnchoredPosition, request.Duration).SetEase(Ease.OutQuad));
            }

            sequence.Join(card.DOFade(1f, request.Duration));
            await TweenAwaiter.AwaitAsync(sequence, request.Duration + request.TimeoutBuffer, cancellationToken);

            rect.anchoredPosition = request.BaseAnchoredPosition;
            SetImageAlpha(card, 1f);
        }

        public async UniTask PlayTransferFallbackAsync(
            Image card,
            Vector3 targetWorldPosition,
            float duration,
            float timeoutBuffer,
            CancellationToken cancellationToken)
        {
            if (card == null)
            {
                return;
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(card.rectTransform.DOMove(targetWorldPosition, duration).SetEase(Ease.InQuad));
            sequence.Join(card.DOFade(0f, duration));
            await TweenAwaiter.AwaitAsync(sequence, duration + timeoutBuffer, cancellationToken);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            image.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}
