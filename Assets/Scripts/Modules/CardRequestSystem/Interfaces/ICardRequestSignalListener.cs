using System.Collections.Generic;
using Modules.CardRequestSystem.Data;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestSignalListener
    {
        void OnCardsRequested(string from, string target, string rank);

        void OnCardsTransferred(string from, string to, IReadOnlyList<CardTransferCardData> cards);

        void OnNoCardsResponse(string from, string target, string rank);

        void OnDefenseDecisionRequested(string askerId, string targetId, string rank, IReadOnlyList<string> defenseOptions);
    }
}
