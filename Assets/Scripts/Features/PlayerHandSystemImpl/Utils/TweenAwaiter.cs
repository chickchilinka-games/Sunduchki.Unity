using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Utils
{
    internal static class TweenAwaiter
    {
        public static async UniTask AwaitAsync(
            Tween tween,
            float timeoutSeconds,
            CancellationToken token = default)
        {
            if (tween == null)
            {
                return;
            }

            token.ThrowIfCancellationRequested();

            tween.SetUpdate(true);
            var completionTcs = new UniTaskCompletionSource();
            var completionSignaled = false;

            void SignalCompletion()
            {
                if (completionSignaled)
                {
                    return;
                }

                completionSignaled = true;
                completionTcs.TrySetResult();
            }

            tween.OnComplete(SignalCompletion);
            tween.OnKill(SignalCompletion);

            if (!tween.IsActive() || tween.IsComplete())
            {
                SignalCompletion();
            }

            var safeTimeout = Mathf.Max(0.1f, timeoutSeconds);
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(safeTimeout), DelayType.UnscaledDeltaTime);

            if (!token.CanBeCanceled)
            {
                var winnerWithoutCancel = await UniTask.WhenAny(completionTcs.Task, timeoutTask);
                if (winnerWithoutCancel != 0 && tween.IsActive())
                {
                    tween.Kill(false);
                }

                return;
            }

            var cancelTask = UniTask.WaitUntilCanceled(token);
            var winner = await UniTask.WhenAny(completionTcs.Task, timeoutTask, cancelTask);
            if (winner == 2)
            {
                if (tween.IsActive())
                {
                    tween.Kill(false);
                }

                token.ThrowIfCancellationRequested();
                return;
            }

            if (winner == 1 && tween.IsActive())
            {
                tween.Kill(false);
            }
        }
    }
}
