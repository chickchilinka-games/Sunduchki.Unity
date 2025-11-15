using System.Collections.Generic;
using Modules.PlayerHand.Data;

namespace Modules.PlayerHand.Interfaces
{
    public interface IPlayerHandSignalListener
    {
        void OnStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards);

        void OnStandardCardAdded(string playerId, StandardCardData card);

        void OnStandardCardRemoved(string playerId, StandardCardData card);

        void OnBonusCardAdded(string playerId, BonusCardData card);

        void OnBonusCardRemoved(string playerId, BonusCardData card);
    }
}
