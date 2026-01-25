using System.Collections.Generic;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using UnityEngine;

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
            var count = cards?.Count ?? 0;
            Debug.Log($"[PlayerHand] Received standard hand snapshot: {count} cards for {playerId}.");
            _stateWriter.ApplyStandardSnapshot(playerId, cards);
        }

        public void OnStandardCardAdded(string playerId, StandardCardData card)
        {
            Debug.Log($"[PlayerHand] Standard card added for {playerId}: {card.Rank} {card.Suit}.");
            _stateWriter.AddStandardCard(playerId, card);
        }

        public void OnStandardCardRemoved(string playerId, StandardCardData card)
        {
            _stateWriter.RemoveStandardCard(playerId, card);
        }

        public void OnBonusCardAdded(string playerId, BonusCardData card)
        {
            Debug.Log($"[PlayerHand] Bonus card added for {playerId}: {card.BonusType}.");
            _stateWriter.AddBonusCard(playerId, card);
        }

        public void OnBonusCardRemoved(string playerId, BonusCardData card)
        {
            _stateWriter.NotifyBonusUsed(playerId, card);
            _stateWriter.RemoveBonusCard(playerId, card);
        }

        public void OnCardsReceived(
            string playerId,
            string source,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards)
        {
            _stateWriter.NotifyCardsReceived(playerId, source, standardCards, bonusCards);
        }

        public void ResetState()
        {
            _stateWriter.ResetAll();
        }
    }
}
