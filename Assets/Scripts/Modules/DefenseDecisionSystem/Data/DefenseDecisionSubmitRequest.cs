namespace Modules.DefenseDecisionSystem.Data
{
    public readonly struct DefenseDecisionSubmitRequest
    {
        public bool UseBonus { get; }
        public string BonusType { get; }

        public DefenseDecisionSubmitRequest(bool useBonus, string bonusType)
        {
            UseBonus = useBonus;
            BonusType = bonusType ?? string.Empty;
        }
    }
}
