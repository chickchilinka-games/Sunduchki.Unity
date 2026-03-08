using System;
using System.Collections.Generic;
using System.Linq;
using Features.PlayerHandSystemImpl.Commands;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.Presentation.Data;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.ViewModel
{
    public sealed class RankStackViewModel : IDisposable
    {
        private readonly ReactiveProperty<IReadOnlyList<StandardCardItemViewModel>> _cards;
        private readonly ReactiveProperty<bool> _canPress;
        private readonly Dictionary<string, StandardCardItemViewModel> _cardsBySuit = new(StringComparer.OrdinalIgnoreCase);
        private readonly IPlayerHandCommands _commands;
        private readonly Subject<Unit> _receiveQueued = new();
        private readonly Queue<RankCardsReceivedEvent> _receiveQueue = new();

        public string Rank { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<StandardCardItemViewModel>> Cards => _cards;
        public ReadOnlyReactiveProperty<bool> CanPress => _canPress;
        public Observable<Unit> ReceiveQueued => _receiveQueued;

        public RankStackViewModel(string rank, IEnumerable<string> suits)
            : this(rank, suits, NullPlayerHandCommands.Instance)
        {
        }

        public RankStackViewModel(string rank, IEnumerable<string> suits, IPlayerHandCommands commands)
        {
            Rank = NormalizeRank(rank);
            _cards = new ReactiveProperty<IReadOnlyList<StandardCardItemViewModel>>(Array.Empty<StandardCardItemViewModel>());
            _canPress = new ReactiveProperty<bool>(false);
            _commands = commands ?? NullPlayerHandCommands.Instance;

            ApplySnapshot(suits);
        }

        public void ApplySnapshot(IEnumerable<string> suits)
        {
            var normalized = new List<string>();
            if (suits != null)
            {
                foreach (var suit in suits)
                {
                    var normalizedSuit = NormalizeSuit(suit);
                    if (!string.IsNullOrEmpty(normalizedSuit))
                    {
                        normalized.Add(normalizedSuit);
                    }
                }
            }

            ApplySuits(normalized);
        }

        public void AddSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            if (_cardsBySuit.ContainsKey(normalized))
            {
                return;
            }

            var copy = _cards.Value?.ToList() ?? new List<StandardCardItemViewModel>();
            var card = new StandardCardItemViewModel(normalized);
            _cardsBySuit[normalized] = card;
            copy.Add(card);
            _cards.Value = copy;
        }

        public void RemoveSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            var copy = _cards.Value?.ToList() ?? new List<StandardCardItemViewModel>();
            var removed = copy.RemoveAll(card =>
                string.Equals(card.Suit, normalized, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                _cardsBySuit.Remove(normalized);
                _cards.Value = copy;
            }
        }

        public void Press()
        {
            if (_canPress.Value)
            {
                _commands.HandleRankPress(Rank);
            }
        }

        public void SetPressable(bool pressable)
        {
            _canPress.Value = pressable;
        }

        public void Dispose()
        {
            _cards?.Dispose();
            _canPress?.Dispose();
            _receiveQueued?.Dispose();
        }

        public void NotifyCardsReceived(string source, IReadOnlyList<string> suits, long eventSeq, bool completedSet)
        {
            var payload = new RankCardsReceivedEvent(
                source,
                suits ?? Array.Empty<string>(),
                eventSeq,
                completedSet);
            _receiveQueue.Enqueue(payload);
            _receiveQueued.OnNext(Unit.Default);
        }

        public bool TryDequeueCardsReceived(out RankCardsReceivedEvent payload)
        {
            if (_receiveQueue.Count == 0)
            {
                payload = default;
                return false;
            }

            payload = _receiveQueue.Dequeue();
            return true;
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

        private void ApplySuits(List<string> suits)
        {
            _cardsBySuit.Clear();
            var next = new List<StandardCardItemViewModel>();
            foreach (var suit in suits)
            {
                if (_cardsBySuit.ContainsKey(suit))
                {
                    continue;
                }

                var card = new StandardCardItemViewModel(suit);
                _cardsBySuit[suit] = card;
                next.Add(card);
            }

            _cards.Value = next;
        }
    }
}
