namespace Modules.DefenseDecisionSystem.Data
{
    public readonly struct DefenseDecisionSubmitPayload
    {
        public string GameId { get; }
        public string TargetPlayerId { get; }
        public bool UseBonus { get; }
        public string BonusType { get; }

        public DefenseDecisionSubmitPayload(string gameId, string targetPlayerId, bool useBonus, string bonusType)
        {
            GameId = gameId ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
            UseBonus = useBonus;
            BonusType = bonusType ?? string.Empty;
        }
    }
}
