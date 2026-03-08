using System;
using System.Collections.Generic;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Model;

namespace Modules.CardRequestSystem.Services
{
    internal class CardRequestInternalService
    {
        private const int MaxProcessedTransferActions = 256;
        private readonly CardRequestModel _model;
        private readonly HashSet<string> _processedTransferActions = new(StringComparer.Ordinal);
        private readonly Queue<string> _processedTransferActionOrder = new();
        private string _lastTransferKey = string.Empty;
        private DateTime _lastTransferAt = DateTime.MinValue;

        public CardRequestInternalService(
            CardRequestModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void OnCardsRequested(string from, string target, string rank)
        {
            var evt = new CardRequestEvent(
                CardRequestEventType.Requested,
                from,
                target,
                rank,
                0,
                Array.Empty<CardTransferCardData>(),
                DateTime.UtcNow);

            _model.SetState(_model.Current.Next(evt));
        }

        public void OnCardsTransferred(string actionId, string from, string to, IReadOnlyList<CardTransferCardData> cards)
        {
            var cardList = cards ?? Array.Empty<CardTransferCardData>();
            if (IsDuplicateTransfer(actionId, from, to, cardList))
            {
                return;
            }

            var rank = cardList.Count > 0 ? cardList[0].Rank : string.Empty;
            var evt = new CardRequestEvent(
                CardRequestEventType.Transferred,
                from,
                to,
                rank,
                cardList.Count,
                cardList,
                DateTime.UtcNow);

            _model.SetState(_model.Current.Next(evt));
        }

        public void OnNoCardsResponse(string from, string target, string rank)
        {
            var evt = new CardRequestEvent(
                CardRequestEventType.Denied,
                from,
                target,
                rank,
                0,
                Array.Empty<CardTransferCardData>(),
                DateTime.UtcNow);

            _model.SetState(_model.Current.Next(evt));
        }

        public void ResetState()
        {
            _processedTransferActions.Clear();
            _processedTransferActionOrder.Clear();
            _lastTransferKey = string.Empty;
            _lastTransferAt = DateTime.MinValue;
            _model.SetState(CardRequestState.Empty);
        }

        private bool IsDuplicateTransfer(string actionId, string from, string to, IReadOnlyList<CardTransferCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(actionId))
            {
                if (!_processedTransferActions.Add(actionId))
                {
                    return true;
                }

                _processedTransferActionOrder.Enqueue(actionId);
                while (_processedTransferActionOrder.Count > MaxProcessedTransferActions)
                {
                    var staleActionId = _processedTransferActionOrder.Dequeue();
                    _processedTransferActions.Remove(staleActionId);
                }

                return false;
            }

            var key = BuildTransferKey(from, to, cards);
            var now = DateTime.UtcNow;
            if (key == _lastTransferKey && (now - _lastTransferAt).TotalMilliseconds < 250)
            {
                return true;
            }

            _lastTransferKey = key;
            _lastTransferAt = now;
            return false;
        }

        private static string BuildTransferKey(string from, string to, IReadOnlyList<CardTransferCardData> cards)
        {
            var parts = new List<string> { from ?? string.Empty, to ?? string.Empty, cards.Count.ToString() };
            foreach (var card in cards)
            {
                parts.Add($"{card.Rank}:{card.Suit}");
            }

            return string.Join("|", parts);
        }
    }
}
