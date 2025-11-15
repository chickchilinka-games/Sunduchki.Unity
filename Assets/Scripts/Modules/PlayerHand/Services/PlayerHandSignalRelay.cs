using System.Collections.Generic;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;

namespace Modules.PlayerHand.Services
{
    internal class PlayerHandSignalRelay : IPlayerHandSignalHandler
    {
        private readonly IPlayerHandStateWriter _stateWriter;

        internal PlayerHandSignalRelay(IPlayerHandStateWriter stateWriter)
        {
            _stateWriter = stateWriter;
        }

        public void OnStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            _stateWriter.ApplyStandardSnapshot(playerId, cards);
        }

        public void OnStandardCardAdded(string playerId, StandardCardData card)
        {
            _stateWriter.AddStandardCard(playerId, card);
        }

        public void OnStandardCardRemoved(string playerId, StandardCardData card)
        {
            _stateWriter.RemoveStandardCard(playerId, card);
        }

        public void OnBonusCardAdded(string playerId, BonusCardData card)
        {
            _stateWriter.AddBonusCard(playerId, card);
        }

        public void OnBonusCardRemoved(string playerId, BonusCardData card)
        {
            _stateWriter.RemoveBonusCard(playerId, card);
        }

        public void ResetState()
        {
            _stateWriter.ResetAll();
        }
    }
}
