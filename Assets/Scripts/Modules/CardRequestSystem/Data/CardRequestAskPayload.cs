namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestAskPayload
    {
        public string GameId { get; }
        public string AskerId { get; }
        public string TargetId { get; }
        public string Rank { get; }

        public CardRequestAskPayload(string gameId, string askerId, string targetId, string rank)
        {
            GameId = gameId ?? string.Empty;
            AskerId = askerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Rank = rank ?? string.Empty;
        }
    }
}
