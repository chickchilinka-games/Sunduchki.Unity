using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace Features.PlayerHandSystemImpl.ViewModel
{
    public sealed class StandardCardViewModel : IDisposable
    {
        private readonly ReactiveProperty<IReadOnlyList<string>> _suits;
        private readonly ReactiveProperty<bool> _canPress;

        public string Rank { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<string>> Suits => _suits;
        public ReadOnlyReactiveProperty<bool> CanPress => _canPress;
        public ReactiveCommand<Unit> Pressed { get; }
        public ReactiveCommand<Unit> SetCompleted { get; }

        public StandardCardViewModel(string rank, IEnumerable<string> suits)
        {
            Rank = NormalizeRank(rank);
            _suits = new ReactiveProperty<IReadOnlyList<string>>(Array.Empty<string>());
            _canPress = new ReactiveProperty<bool>(false);
            Pressed = new ReactiveCommand<Unit>();
            SetCompleted = new ReactiveCommand<Unit>();

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

            _suits.Value = normalized;
        }

        public void AddSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            var copy = _suits.Value?.ToList() ?? new List<string>();
            copy.Add(normalized);
            _suits.Value = copy;
        }

        public void RemoveSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            var copy = _suits.Value?.ToList() ?? new List<string>();
            if (copy.Remove(normalized))
            {
                _suits.Value = copy;
            }
        }

        public void Press()
        {
            if (_canPress.Value)
            {
                Pressed.Execute(Unit.Default);
            }
        }

        public void SetPressable(bool pressable)
        {
            _canPress.Value = pressable;
        }

        public void Dispose()
        {
            _suits?.Dispose();
            _canPress?.Dispose();
            Pressed?.Dispose();
            SetCompleted?.Dispose();
        }

        public void MarkSetCompleted()
        {
            SetCompleted.Execute(Unit.Default);
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
    }
}
