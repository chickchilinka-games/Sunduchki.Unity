using System.Collections.Generic;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestSignalListener
    {
        void OnCardsRequested(string from, string target, string rank);

        void OnCardsTransferred(string from, string to, string rank, int count);

        void OnNoCardsResponse(string from, string target, string rank);

        void OnDefenseDecisionRequested(string askerId, string targetId, string rank, IReadOnlyList<string> defenseOptions);
    }
}
