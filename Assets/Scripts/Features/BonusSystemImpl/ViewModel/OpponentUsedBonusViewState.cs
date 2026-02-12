namespace Features.BonusSystemImpl.ViewModel
{
    public readonly struct OpponentUsedBonusViewState
    {
        public static readonly OpponentUsedBonusViewState Hidden = new(false, string.Empty, null);

        public bool IsVisible { get; }
        public string BonusType { get; }
        public float? HideDelay { get; }

        public OpponentUsedBonusViewState(bool isVisible, string bonusType, float? hideDelay)
        {
            IsVisible = isVisible;
            BonusType = bonusType ?? string.Empty;
            HideDelay = hideDelay;
        }
    }
}
