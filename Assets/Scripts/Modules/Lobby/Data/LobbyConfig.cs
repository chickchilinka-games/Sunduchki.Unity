namespace Modules.Lobby.Data
{
    public struct LobbyConfig
    {
        public string GameId { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool? IsHost { get; set; }
        public int? DeckCount { get; set; }
        public int? TotalCards { get; set; }
    }
}
