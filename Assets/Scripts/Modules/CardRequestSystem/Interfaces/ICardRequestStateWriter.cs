using System.Collections.Generic;
using Modules.CardRequestSystem.Data;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestStateWriter
    {
        void RegisterRequest(string from, string target, string rank);

        void RegisterTransfer(string from, string to, IReadOnlyList<CardTransferCardData> cards);

        void RegisterNoCards(string from, string target, string rank);

        void Reset();
    }
}
