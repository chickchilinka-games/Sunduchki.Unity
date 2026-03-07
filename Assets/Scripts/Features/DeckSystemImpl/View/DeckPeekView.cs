using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Features.PlayerHandSystemImpl.View;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.DeckSystem.Services;
using Modules.Lobby.Services;
using R3;
using UnityEngine;
using Zenject;

namespace Features.DeckSystemImpl.View
{
    public sealed class DeckPeekView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Transform _container;
        [SerializeField] private int _maxCards = 3;

        private DeckService _deckService;
        private LobbyService _lobbyService;
        private RankStackViewPool _standardPool;
        private BonusCardViewPool _bonusPool;
        private IDisposable _subscription;
        private readonly List<ViewEntry> _entries = new();
        private CancellationTokenSource _cts;

        [Inject]
        public void Construct(
            DeckService deckService,
            LobbyService lobbyService,
            RankStackViewPool standardPool,
            BonusCardViewPool bonusPool)
        {
            _deckService = deckService ?? throw new ArgumentNullException(nameof(deckService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _standardPool = standardPool ?? throw new ArgumentNullException(nameof(standardPool));
            _bonusPool = bonusPool ?? throw new ArgumentNullException(nameof(bonusPool));
        }

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_container == null)
            {
                _container = transform;
            }

            Hide();
        }

        private void OnEnable()
        {
            _subscription = _deckService.State.Subscribe(OnDeckStateChanged);
            OnDeckStateChanged(_deckService.Current);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ClearEntries();
            Hide();
        }

        private void OnDeckStateChanged(DeckState state)
        {
            var peek = state.PeekInfo;
            if (!peek.HasValue || peek.Cards == null || peek.Cards.Count == 0)
            {
                ClearEntries();
                Hide();
                return;
            }

            var localId = _lobbyService.GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(localId) ||
                !string.Equals(peek.PlayerId, localId, StringComparison.Ordinal))
            {
                ClearEntries();
                Hide();
                return;
            }

            Show(peek.Cards);
        }

        private void Show(IReadOnlyList<DeckPeekCardData> cards)
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            BuildViewsAsync(cards, _cts.Token).Forget();
        }

        private async UniTaskVoid BuildViewsAsync(IReadOnlyList<DeckPeekCardData> cards, CancellationToken token)
        {
            ClearEntries();

            if (cards == null || cards.Count == 0)
            {
                return;
            }

            var limit = Mathf.Max(1, _maxCards);
            var count = Math.Min(cards.Count, limit);

            for (var i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var card = cards[i];
                if (card.IsBonus)
                {
                    if (_bonusPool == null)
                    {
                        Debug.LogWarning("[DeckPeek] Bonus pool is not configured.");
                        break;
                    }

                    var vm = new BonusCardViewModel(card.BonusType);
                    var view = _bonusPool.Spawn(_container, vm);
                    view.SetTint(Color.white);
                    _entries.Add(new ViewEntry(view, vm, _bonusPool));
                }
                else
                {
                    if (_standardPool == null)
                    {
                        Debug.LogWarning("[DeckPeek] Standard pool is not configured.");
                        break;
                    }

                    var rank = NormalizeRank(card.Rank);
                    var suit = NormalizeSuit(card.Suit);
                    var vm = new RankStackViewModel(rank, new[] { suit });
                    var view = _standardPool.Spawn(_container, vm);
                    view.SetTint(Color.white);
                    _entries.Add(new ViewEntry(view, vm, _standardPool));
                }
            }
        }

        private void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
            {
                entry.Dispose();
            }

            _entries.Clear();
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
        }

        private sealed class ViewEntry
        {
            private readonly Component _view;
            private readonly IDisposable _viewModel;
            private readonly RankStackViewPool _standardPool;
            private readonly BonusCardViewPool _bonusPool;

            public ViewEntry(Component view, IDisposable viewModel, RankStackViewPool standardPool)
            {
                _view = view;
                _viewModel = viewModel;
                _standardPool = standardPool;
            }

            public ViewEntry(Component view, IDisposable viewModel, BonusCardViewPool bonusPool)
            {
                _view = view;
                _viewModel = viewModel;
                _bonusPool = bonusPool;
            }

            public void Dispose()
            {
                switch (_view)
                {
                    case RankStackView standard:
                        standard.ResetView();
                        _standardPool?.Despawn(standard);
                        break;
                    case BonusCardView bonus:
                        bonus.ResetView();
                        _bonusPool?.Despawn(bonus);
                        break;
                }

                _viewModel?.Dispose();
            }
        }
    }
}
