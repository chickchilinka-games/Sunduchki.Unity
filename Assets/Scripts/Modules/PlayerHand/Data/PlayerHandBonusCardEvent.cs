namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandBonusCardEvent
    {
        public string PlayerId { get; }
        public BonusCardData Card { get; }

        public PlayerHandBonusCardEvent(string playerId, BonusCardData card)
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
        }
    }
}
