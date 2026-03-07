using System;
using System.Collections.Generic;
using System.Linq;
using Modules.PlayerHand.Data;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    internal sealed class HandReceiveBuffer
    {
        private readonly float _pendingStandardReceiveMaxAgeSeconds;
        private readonly float _pendingBonusReceiveMaxAgeSeconds;
        private readonly List<PendingStandardReceive> _pendingStandardReceives = new();
        private readonly Queue<PendingBonusReceive> _pendingBonusReceives = new();

        public readonly struct PendingStandardDispatch
        {
            public string Rank { get; }
            public string Source { get; }
            public long EventSeq { get; }
            public bool CompletedSet { get; }
            public IReadOnlyList<string> Suits { get; }

            public PendingStandardDispatch(string rank, string source, long eventSeq, bool completedSet, IReadOnlyList<string> suits)
            {
                Rank = rank;
                Source = source;
                EventSeq = eventSeq;
                CompletedSet = completedSet;
                Suits = suits;
            }
        }

        private readonly struct PendingStandardReceive
        {
            public string Rank { get; }
            public string Suit { get; }
            public string Source { get; }
            public long EventSeq { get; }
            public bool CompletedSet { get; }
            public float CreatedAt { get; }

            public PendingStandardReceive(
                string rank,
                string suit,
                string source,
                long eventSeq,
                bool completedSet,
                float createdAt)
            {
                Rank = rank;
                Suit = suit;
                Source = source;
                EventSeq = eventSeq;
                CompletedSet = completedSet;
                CreatedAt = createdAt;
            }
        }

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

        public HandReceiveBuffer(float pendingStandardReceiveMaxAgeSeconds, float pendingBonusReceiveMaxAgeSeconds)
        {
            _pendingStandardReceiveMaxAgeSeconds = pendingStandardReceiveMaxAgeSeconds;
            _pendingBonusReceiveMaxAgeSeconds = pendingBonusReceiveMaxAgeSeconds;
        }

        public void Clear()
        {
            _pendingStandardReceives.Clear();
            _pendingBonusReceives.Clear();
        }

        public void EnqueuePendingStandardReceive(
            string rank,
            IReadOnlyList<string> suits,
            string source,
            long eventSeq,
            bool completedSet)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var now = Time.unscaledTime;
            if (suits != null && suits.Count > 0)
            {
                foreach (var suit in suits)
                {
                    var suitKey = NormalizeSuit(suit);
                    if (string.IsNullOrWhiteSpace(suitKey))
                    {
                        continue;
                    }

                    _pendingStandardReceives.Add(new PendingStandardReceive(
                        rankKey,
                        suitKey,
                        source,
                        eventSeq,
                        completedSet,
                        now));
                }

                return;
            }

            _pendingStandardReceives.Add(new PendingStandardReceive(
                rankKey,
                string.Empty,
                source,
                eventSeq,
                completedSet,
                now));
        }

        public void FlushPendingStandardReceives(
            Func<string, bool> canDispatch,
            Func<PendingStandardDispatch, bool> dispatch)
        {
            if (_pendingStandardReceives.Count == 0 || canDispatch == null || dispatch == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            _pendingStandardReceives.RemoveAll(item => now - item.CreatedAt > _pendingStandardReceiveMaxAgeSeconds);
            if (_pendingStandardReceives.Count == 0)
            {
                return;
            }

            var grouped = _pendingStandardReceives
                .GroupBy(item => new { item.Rank, item.Source, item.EventSeq, item.CompletedSet })
                .ToList();

            var consumed = new HashSet<PendingStandardReceive>();
            foreach (var group in grouped)
            {
                if (!canDispatch(group.Key.Rank))
                {
                    continue;
                }

                var suits = group
                    .Select(item => item.Suit)
                    .Where(suit => !string.IsNullOrWhiteSpace(suit))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var standardDispatch = new PendingStandardDispatch(
                    group.Key.Rank,
                    group.Key.Source,
                    group.Key.EventSeq,
                    group.Key.CompletedSet,
                    suits);

                if (!dispatch(standardDispatch))
                {
                    continue;
                }

                foreach (var item in group)
                {
                    consumed.Add(item);
                }
            }

            if (consumed.Count == 0)
            {
                return;
            }

            _pendingStandardReceives.RemoveAll(consumed.Contains);
        }

        public IReadOnlyList<string> GetPendingStandardSuitsForRank(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey) || _pendingStandardReceives.Count == 0)
            {
                return Array.Empty<string>();
            }

            return _pendingStandardReceives
                .Where(item => string.Equals(item.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Suit)
                .Where(suit => !string.IsNullOrWhiteSpace(suit))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
        }

        private static string NormalizeBonus(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType) ? string.Empty : bonusType.Trim();
        }
    }
}
