using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BonusCardSet3DView : MonoBehaviour
    {
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private BoardCard3DView _cardPrefab;
        [SerializeField, Min(0f)] private float _cardSpacingX = 0.34f;
        [SerializeField] private float _cardDepthStep = -0.002f;

        private readonly List<BoardCard3DView> _cards = new();

        public string BonusType { get; private set; } = string.Empty;

        private void Awake()
        {
            if (_cardsRoot == null)
            {
                _cardsRoot = transform;
            }
        }

        public async UniTask BindAsync(
            string bonusType,
            int count,
            BoardCardSpriteResolver resolver,
            CancellationToken cancellationToken = default)
        {
            BonusType = NormalizeBonusType(bonusType);
            var safeCount = Math.Max(0, count);
            EnsureCardCount(safeCount);

            for (var i = 0; i < safeCount; i++)
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
                await cardView.SetBonusCardAsync(BonusType, resolver, cancellationToken);
            }
        }

        public void ResetView()
        {
            BonusType = string.Empty;
            ClearCards();
        }

        private void EnsureCardCount(int count)
        {
            if (_cardPrefab == null)
            {
                Debug.LogWarning("[PlayerHand3D] Bonus set card prefab is not assigned.");
                ClearCards();
                return;
            }

            var targetCount = Math.Max(0, count);
            while (_cards.Count < targetCount)
            {
                var card = Instantiate(_cardPrefab, _cardsRoot);
                card.name = $"BonusCard_{_cards.Count}";
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

        private static string NormalizeBonusType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? string.Empty
                : bonusType.Trim();
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
