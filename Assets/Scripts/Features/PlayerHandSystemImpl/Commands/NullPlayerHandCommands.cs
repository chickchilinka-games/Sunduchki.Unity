using Features.PlayerHandSystemImpl.Interfaces;

namespace Features.PlayerHandSystemImpl.Commands
{
    internal sealed class NullPlayerHandCommands : IPlayerHandCommands
    {
        public static readonly NullPlayerHandCommands Instance = new();

        public void HandleRankPress(string rank)
        {
        }
    }
}
