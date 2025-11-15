using Modules.CardRequestSystem.Interfaces;

namespace Modules.CardRequestSystem.Services
{
    public class CardRequestSignalRelay : ICardRequestSignalHandler
    {
        private readonly ICardRequestStateWriter _stateWriter;

        public CardRequestSignalRelay(ICardRequestStateWriter stateWriter)
        {
            _stateWriter = stateWriter;
        }

        public void OnCardsRequested(string from, string target, string rank)
        {
            _stateWriter.RegisterRequest(from, target, rank);
        }

        public void OnCardsTransferred(string from, string to, string rank, int count)
        {
            _stateWriter.RegisterTransfer(from, to, rank, count);
        }

        public void OnNoCardsResponse(string from, string target, string rank)
        {
            _stateWriter.RegisterNoCards(from, target, rank);
        }

        public void ResetState()
        {
            _stateWriter.Reset();
        }
    }
}
