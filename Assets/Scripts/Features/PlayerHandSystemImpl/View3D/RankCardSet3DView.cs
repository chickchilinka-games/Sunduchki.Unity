using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class RankCardSet3DView : MonoBehaviour
    {
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private BoardCard3DView _cardPrefab;
        [SerializeField, Min(0f)] private float _cardSpacingX = 0.34f;
        [SerializeField] private float _cardDepthStep = -0.002f;

        private readonly List<BoardCard3DView> _cards = new();

        public string Rank { get; private set; } = string.Empty;

        private void Awake()
        {
            if (_cardsRoot == null)
            {
                _cardsRoot = transform;
            }
        }

        public async UniTask BindAsync(
            string rank,
            IReadOnlyList<string> suits,
            BoardCardSpriteResolver resolver,
            CancellationToken cancellationToken = default)
        {
            Rank = NormalizeRank(rank);
            var normalizedSuits = NormalizeSuits(suits);
            EnsureCardCount(normalizedSuits.Count);

            for (var i = 0; i < normalizedSuits.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var cardView = _cards[i];
                if (cardView == null)
                {
                    continue;
                }

                cardView.transform.localPosition = new Vector3(i * _cardSpacingX, 0f, i * _cardDepthStep);
                await cardView.SetStandardCardAsync(Rank, normalizedSuits[i], resolver, cancellationToken);
            }
        }

        public void ResetView()
        {
            Rank = string.Empty;
            ClearCards();
        }

        private void EnsureCardCount(int count)
        {
            if (_cardPrefab == null)
            {
                Debug.LogWarning("[PlayerHand3D] Rank set card prefab is not assigned.");
                ClearCards();
                return;
            }

            var targetCount = Math.Max(0, count);
            while (_cards.Count < targetCount)
            {
                var card = Instantiate(_cardPrefab, _cardsRoot);
                card.name = $"Card_{_cards.Count}";
                _cards.Add(card);
            }

            while (_cards.Count > targetCount)
            {
                var lastIndex = _cards.Count - 1;
                var stale = _cards[lastIndex];
                _cards.RemoveAt(lastIndex);
                if (stale != null)
                {
                    stale.ResetView();
                    DestroySafe(stale.gameObject);
                }
            }
        }

        private void ClearCards()
        {
            for (var i = _cards.Count - 1; i >= 0; i--)
            {
                var card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                card.ResetView();
                DestroySafe(card.gameObject);
            }

            _cards.Clear();
        }

        private static List<string> NormalizeSuits(IReadOnlyList<string> suits)
        {
            var result = new List<string>();
            if (suits == null)
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < suits.Count; i++)
            {
                var normalized = NormalizeSuit(suits[i]);
                if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
                {
                    continue;
                }

                result.Add(normalized);
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? string.Empty
                : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit)
                ? string.Empty
                : suit.Trim().ToLowerInvariant();
        }

        private static void DestroySafe(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }
    }
}
