using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace Features.PlayerHandSystemImpl.ViewModel
{
    public sealed class StandardCardViewModel : IDisposable
    {
        private readonly List<string> _suits;
        private readonly ReactiveProperty<string> _lastSuit;
        private readonly ReactiveProperty<int> _amount;
        private readonly ReactiveProperty<bool> _canPress;

        public string Rank { get; }
        public IReadOnlyList<string> Suits => _suits;
        public ReadOnlyReactiveProperty<string> LastSuit => _lastSuit;
        public ReadOnlyReactiveProperty<int> Amount => _amount;
        public ReadOnlyReactiveProperty<bool> CanPress => _canPress;
        public ReactiveCommand<Unit> Pressed { get; }

        public StandardCardViewModel(string rank, IEnumerable<string> suits)
        {
            Rank = NormalizeRank(rank);
            _suits = new List<string>();
            _lastSuit = new ReactiveProperty<string>(string.Empty);
            _amount = new ReactiveProperty<int>(0);
            _canPress = new ReactiveProperty<bool>(false);
            Pressed = new ReactiveCommand<Unit>();

            ApplySnapshot(suits);
        }

        public void ApplySnapshot(IEnumerable<string> suits)
        {
            _suits.Clear();
            if (suits != null)
            {
                foreach (var suit in suits)
                {
                    var normalized = NormalizeSuit(suit);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        _suits.Add(normalized);
                    }
                }
            }

            _amount.Value = _suits.Count;
            _lastSuit.Value = _suits.Count > 0 ? _suits[^1] : string.Empty;
        }

        public void AddSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            _suits.Add(normalized);
            _lastSuit.Value = normalized;
            _amount.Value = _suits.Count;
        }

        public void RemoveSuit(string suit)
        {
            var normalized = NormalizeSuit(suit);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            if (_suits.Remove(normalized))
            {
                _amount.Value = _suits.Count;
                _lastSuit.Value = _suits.Count > 0 ? _suits[^1] : string.Empty;
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
            _lastSuit?.Dispose();
            _amount?.Dispose();
            _canPress?.Dispose();
            Pressed?.Dispose();
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
