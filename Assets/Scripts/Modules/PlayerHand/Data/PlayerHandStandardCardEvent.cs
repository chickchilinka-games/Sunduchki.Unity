namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandStandardCardEvent
    {
        public string PlayerId { get; }
        public StandardCardData Card { get; }

        public PlayerHandStandardCardEvent(string playerId, StandardCardData card)
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
        }
    }
}
