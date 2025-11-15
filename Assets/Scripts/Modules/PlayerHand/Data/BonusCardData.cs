namespace Modules.PlayerHand.Data
{
    public readonly struct BonusCardData
    {
        public string BonusType { get; }

        public BonusCardData(string bonusType)
        {
            BonusType = bonusType ?? string.Empty;
        }
    }
}
