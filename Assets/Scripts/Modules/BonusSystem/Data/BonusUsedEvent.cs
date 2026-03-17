namespace Modules.BonusSystem.Data
{
    public readonly struct BonusUsedEvent
    {
        public string PlayerId { get; }
        public string BonusType { get; }
        public string TargetPlayerId { get; }
        public long EventSeq { get; }

        public BonusUsedEvent(string playerId, string bonusType, string targetPlayerId, long eventSeq = 0)
        {
            PlayerId = playerId ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
            EventSeq = eventSeq;
        }
    }
}
