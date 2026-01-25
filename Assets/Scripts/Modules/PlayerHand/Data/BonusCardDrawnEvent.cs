namespace Modules.PlayerHand.Data
{
    public readonly struct BonusCardDrawnEvent
    {
        public string PlayerId { get; }
        public BonusCardData Card { get; }

        public BonusCardDrawnEvent(string playerId, BonusCardData card)
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
        }
    }
}
