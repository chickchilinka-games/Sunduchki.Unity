namespace Modules.Lobby.Data
{
    public struct JoinGameOptions
    {
        public string Token { get; }
        public string Name { get; }

        public JoinGameOptions(string token, string name)
        {
            Token = token;
            Name = name;
        }
    }
}
