namespace Modules.BonusSystem.Data
{
    public readonly struct BonusUsePayload
    {
        public string GameId { get; }
        public string PlayerId { get; }
        public string BonusType { get; }
        public string TargetPlayerId { get; }

        public BonusUsePayload(string gameId, string playerId, string bonusType, string targetPlayerId)
        {
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
        }
    }
}
