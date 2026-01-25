using System.Collections.Generic;
using Modules.PlayerHand.Data;

namespace Modules.PlayerHand.Interfaces
{
    internal interface IPlayerHandStateWriter
    {
        void ApplyStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards);

        void AddStandardCard(string playerId, StandardCardData card);

        void RemoveStandardCard(string playerId, StandardCardData card);

        void AddBonusCard(string playerId, BonusCardData card);

        void RemoveBonusCard(string playerId, BonusCardData card);

        void NotifyBonusUsed(string playerId, BonusCardData card);

        void NotifyCardsReceived(string playerId, string source, IReadOnlyList<StandardCardData> standardCards, IReadOnlyList<BonusCardData> bonusCards);

        void ClearHand(string playerId);

        void ResetAll();
    }
}
