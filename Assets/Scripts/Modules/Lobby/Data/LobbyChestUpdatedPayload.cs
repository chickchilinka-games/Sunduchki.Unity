namespace Modules.Lobby.Data
{
    public readonly struct LobbyChestUpdatedPayload
    {
        public string PlayerId { get; }
        public int ChestCount { get; }
        public string Rank { get; }

        public LobbyChestUpdatedPayload(string playerId, int chestCount, string rank)
        {
            PlayerId = playerId ?? string.Empty;
            ChestCount = chestCount < 0 ? 0 : chestCount;
            Rank = rank ?? string.Empty;
        }
    }
}
