namespace Modules.PlayerHand.Data
{
    public readonly struct BonusCardUsageEvent
    {
        public string PlayerId { get; }
        public string BonusType { get; }

        public BonusCardUsageEvent(string playerId, string bonusType)
        {
            PlayerId = playerId ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
        }
    }
}
