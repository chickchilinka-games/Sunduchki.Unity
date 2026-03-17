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
        private readonly Subject<Unit> _commandQueued = new();
        private readonly LinkedList<RankStackCommand> _commandsQueue = new();
        private long _snapshotRevision;

        public string Rank { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<StandardCardItemViewModel>> Cards => _cards;
        public ReadOnlyReactiveProperty<bool> CanPress => _canPress;
        public Observable<Unit> CommandQueued => _commandQueued;
        public bool HasPendingCommands => _commandsQueue.Count > 0;

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

            ApplySnapshot(suits, NextSnapshotRevision());
        }

        public void ApplySnapshot(IEnumerable<string> suits)
        {
            ApplySnapshot(suits, NextSnapshotRevision());
        }

        public void ApplySnapshot(IEnumerable<string> suits, long revision)
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
            if (revision > _snapshotRevision)
            {
                _snapshotRevision = revision;
            }

            EnqueueCommand(RankStackCommand.Snapshot(normalized.ToArray(), _snapshotRevision), coalesceSnapshot: true);
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
            _commandQueued?.Dispose();
        }

        public void EnqueueReceive(string source, IReadOnlyList<string> suits, long eventSeq, bool completedSet)
        {
            var normalizedSuits = NormalizeSuits(suits);
            if (normalizedSuits.Count > 0)
            {
                EnqueueCommand(RankStackCommand.Receive(source, normalizedSuits, eventSeq, completedSet: false));
            }

            if (completedSet)
            {
                EnqueueCommand(RankStackCommand.SetComplete(eventSeq));
            }
        }

        public void EnqueueTransferOut(IReadOnlyList<string> suits, int count, string actionId)
        {
            var normalizedSuits = NormalizeSuits(suits);
            if (normalizedSuits.Count == 0 && count <= 0)
            {
                return;
            }

            EnqueueCommand(RankStackCommand.TransferOut(normalizedSuits, count, actionId));
        }

        internal bool TryDequeueCommand(out RankStackCommand command)
        {
            if (_commandsQueue.Count == 0)
            {
                command = default;
                return false;
            }

            command = _commandsQueue.First.Value;
            _commandsQueue.RemoveFirst();
            return true;
        }

        public bool IsSuitPendingReceive(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            foreach (var command in _commandsQueue)
            {
                if (command.Type == RankStackCommandType.ApplySnapshot)
                {
                    break;
                }

                if (command.Type != RankStackCommandType.Receive || command.Suits == null)
                {
                    continue;
                }

                for (var i = 0; i < command.Suits.Count; i++)
                {
                    if (string.Equals(NormalizeSuit(command.Suits[i]), normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private void EnqueueCommand(RankStackCommand command, bool coalesceSnapshot = false)
        {
            if (coalesceSnapshot &&
                command.Type == RankStackCommandType.ApplySnapshot &&
                _commandsQueue.Last != null &&
                _commandsQueue.Last.Value.Type == RankStackCommandType.ApplySnapshot)
            {
                _commandsQueue.Last.Value = command;
            }
            else
            {
                _commandsQueue.AddLast(command);
            }

            _commandQueued.OnNext(Unit.Default);
        }

        private long NextSnapshotRevision()
        {
            _snapshotRevision++;
            return _snapshotRevision;
        }

        private static List<string> NormalizeSuits(IReadOnlyList<string> suits)
        {
            var normalized = new List<string>();
            if (suits == null)
            {
                return normalized;
            }

            for (var i = 0; i < suits.Count; i++)
            {
                var suit = NormalizeSuit(suits[i]);
                if (string.IsNullOrWhiteSpace(suit))
                {
                    continue;
                }

                normalized.Add(suit);
            }

            return normalized;
        }
    }
}
