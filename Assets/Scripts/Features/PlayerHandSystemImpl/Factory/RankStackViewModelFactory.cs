using System;
using System.Collections.Generic;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;

namespace Features.PlayerHandSystemImpl.Factory
{
    public sealed class RankStackViewModelFactory : IRankStackViewModelFactory
    {
        private readonly IPlayerHandCommands _commands;

        public RankStackViewModelFactory(IPlayerHandCommands commands)
        {
            _commands = commands;
        }

        public RankStackViewModel Create(string rank, IEnumerable<string> suits)
        {
            return new RankStackViewModel(rank, suits, _commands);
        }
    }
}
