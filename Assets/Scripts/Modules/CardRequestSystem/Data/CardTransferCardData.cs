namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardTransferCardData
    {
        public string Rank { get; }
        public string Suit { get; }

        public CardTransferCardData(string rank, string suit)
        {
            Rank = rank ?? string.Empty;
            Suit = suit ?? string.Empty;
        }
    }
}
