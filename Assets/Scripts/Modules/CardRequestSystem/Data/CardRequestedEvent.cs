namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestedEvent
    {
        public string FromPlayerId { get; }
        public string TargetPlayerId { get; }
        public string Rank { get; }

        public CardRequestedEvent(string fromPlayerId, string targetPlayerId, string rank)
        {
            FromPlayerId = fromPlayerId ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
            Rank = rank ?? string.Empty;
        }
    }
}
