using System;
using System.Collections.Generic;
using Modules.PlayerHand.Data;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    internal sealed class HandReceiveBuffer
    {
        private readonly float _pendingBonusReceiveMaxAgeSeconds;
        private readonly Queue<PendingBonusReceive> _pendingBonusReceives = new();

        private readonly struct PendingBonusReceive
        {
            public string BonusType { get; }
            public string Source { get; }
            public float CreatedAt { get; }

            public PendingBonusReceive(string bonusType, string source, float createdAt)
            {
                BonusType = bonusType ?? string.Empty;
                Source = source ?? string.Empty;
                CreatedAt = createdAt;
            }
        }

        public HandReceiveBuffer(float pendingBonusReceiveMaxAgeSeconds)
        {
            _pendingBonusReceiveMaxAgeSeconds = pendingBonusReceiveMaxAgeSeconds;
        }

        public void Clear()
        {
            _pendingBonusReceives.Clear();
        }

        public void EnqueuePendingBonusReceive(string bonusType, string source)
        {
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                return;
            }

            _pendingBonusReceives.Enqueue(new PendingBonusReceive(
                NormalizeBonus(bonusType),
                source,
                Time.unscaledTime));
        }

        public void FlushPendingBonusReceives(
            Func<string, RectTransform> resolveOrigin,
            Func<BonusCardData, RectTransform, bool> tryAnimate)
        {
            if (_pendingBonusReceives.Count == 0 || resolveOrigin == null || tryAnimate == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            var pendingCount = _pendingBonusReceives.Count;
            for (var i = 0; i < pendingCount; i++)
            {
                var pending = _pendingBonusReceives.Dequeue();
                if (now - pending.CreatedAt > _pendingBonusReceiveMaxAgeSeconds)
                {
                    continue;
                }

                var origin = resolveOrigin(pending.Source);
                if (origin == null ||
                    !tryAnimate(new BonusCardData(pending.BonusType), origin))
                {
                    _pendingBonusReceives.Enqueue(pending);
                }
            }
        }

        private static string NormalizeBonus(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType) ? string.Empty : bonusType.Trim();
        }
    }
}
