namespace Modules.BonusSystem.Data
{
    public readonly struct BonusUseRequest
    {
        public string BonusType { get; }
        public string TargetPlayerId { get; }

        public BonusUseRequest(string bonusType, string targetPlayerId)
        {
            BonusType = bonusType ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
        }
    }
}
