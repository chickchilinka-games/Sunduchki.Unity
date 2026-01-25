namespace Modules.PlayerHand.Data
{
    public readonly struct StandardCardDrawnEvent
    {
        public string PlayerId { get; }
        public StandardCardData Card { get; }

        public StandardCardDrawnEvent(string playerId, StandardCardData card)
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
        }
    }
}
