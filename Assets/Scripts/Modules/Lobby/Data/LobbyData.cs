namespace Modules.Lobby.Data
{
    public struct LobbyData
    {
        public string GameId { get; set; }
        public string PlayerId { get; set; }
        public bool? IsHost { get; set; }
        public int? DeckCount { get; set; }
        public int? TotalCards { get; set; }
    }
}
