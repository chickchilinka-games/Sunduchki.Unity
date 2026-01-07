using System;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.CardRequestSystem.Model;
using R3;

namespace Modules.CardRequestSystem.Services
{
    internal class CardRequestService : ICardRequestService, ICardRequestStateWriter
    {
        private readonly CardRequestModel _model;

        public CardRequestService(CardRequestModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<CardRequestState> State => _model.State;

        public CardRequestState Current => _model.Current;

        public void RegisterRequest(string from, string target, string rank)
        {
            var evt = new CardRequestEvent(
                CardRequestEventType.Requested,
                from,
                target,
                rank,
                0,
                DateTime.UtcNow);
            _model.SetState(_model.Current.Next(evt));
        }

        public void RegisterTransfer(string from, string to, string rank, int count)
        {
            var evt = new CardRequestEvent(
                CardRequestEventType.Transferred,
                from,
                to,
                rank,
                count,
                DateTime.UtcNow);
            _model.SetState(_model.Current.Next(evt));
        }

        public void RegisterNoCards(string from, string target, string rank)
        {
            var evt = new CardRequestEvent(
                CardRequestEventType.Denied,
                from,
                target,
                rank,
                0,
                DateTime.UtcNow);
            _model.SetState(_model.Current.Next(evt));
        }

        public void Reset()
        {
            _model.SetState(CardRequestState.Empty);
        }
    }
}
