using System;
using R3;

namespace Features.PlayerHandSystemImpl.ViewModel
{
    public sealed class BonusCardViewModel : IDisposable
    {
        private readonly ReactiveProperty<bool> _canUse;

        public string BonusCardType { get; }
        public ReadOnlyReactiveProperty<bool> CanUse => _canUse;
        public string InfoTitle { get; private set; }
        public string InfoText { get; private set; }
        public ReactiveCommand<Unit> Use { get; }

        public BonusCardViewModel(string bonusCardType)
        {
            BonusCardType = NormalizeType(bonusCardType);
            _canUse = new ReactiveProperty<bool>(false);
            InfoTitle = string.Empty;
            InfoText = string.Empty;
            Use = new ReactiveCommand<Unit>();
        }

        public void SetCanUse(bool canUse)
        {
            _canUse.Value = canUse;
        }

        public void SetInfo(string title, string text)
        {
            InfoTitle = title ?? string.Empty;
            InfoText = text ?? string.Empty;
        }

        public void TriggerUse()
        {
            if (_canUse.Value)
            {
                Use.Execute(Unit.Default);
            }
        }

        public void Dispose()
        {
            _canUse?.Dispose();
            Use?.Dispose();
        }

        private static string NormalizeType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? string.Empty
                : bonusType.Trim();
        }
    }
}
