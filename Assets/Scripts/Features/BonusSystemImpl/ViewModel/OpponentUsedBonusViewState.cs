namespace Features.BonusSystemImpl.ViewModel
{
    public readonly struct OpponentUsedBonusViewState
    {
        public static readonly OpponentUsedBonusViewState Hidden = new(false, string.Empty, null, 0);

        public bool IsVisible { get; }
        public string BonusType { get; }
        public float? HideDelay { get; }
        public int Revision { get; }

        public OpponentUsedBonusViewState(bool isVisible, string bonusType, float? hideDelay, int revision)
        {
            IsVisible = isVisible;
            BonusType = bonusType ?? string.Empty;
            HideDelay = hideDelay;
            Revision = revision;
        }
    }
}
