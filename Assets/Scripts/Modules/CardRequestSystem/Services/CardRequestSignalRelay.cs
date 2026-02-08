using System;
using System.Collections.Generic;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;

namespace Modules.CardRequestSystem.Services
{
    public class CardRequestSignalRelay : ICardRequestSignalHandler
    {
        private readonly ICardRequestStateWriter _stateWriter;
        private readonly IDefenseDecisionPromptWriter _defensePromptWriter;
        private string _lastTransferKey = string.Empty;
        private DateTime _lastTransferAt = DateTime.MinValue;

        public CardRequestSignalRelay(ICardRequestStateWriter stateWriter, IDefenseDecisionPromptWriter defensePromptWriter)
        {
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
            _defensePromptWriter = defensePromptWriter;
        }

        public void OnCardsRequested(string from, string target, string rank)
        {
            _stateWriter.RegisterRequest(from, target, rank);
        }

        public void OnCardsTransferred(string from, string to, IReadOnlyList<CardTransferCardData> cards)
        {
            var cardList = cards ?? Array.Empty<CardTransferCardData>();
            if (IsDuplicateTransfer(from, to, cardList))
            {
                return;
            }

            _stateWriter.RegisterTransfer(from, to, cardList);
        }

        public void OnNoCardsResponse(string from, string target, string rank)
        {
            _stateWriter.RegisterNoCards(from, target, rank);
        }

        public void OnDefenseDecisionRequested(string askerId, string targetId, string rank, IReadOnlyList<string> defenseOptions)
        {
            if (_defensePromptWriter == null)
            {
                return;
            }

            var prompt = new DefenseDecisionPrompt(askerId, targetId, rank, defenseOptions);
            _defensePromptWriter.PublishPrompt(prompt);
        }

        public void ResetState()
        {
            _stateWriter.Reset();
            _defensePromptWriter?.ClearPrompt();
        }

        private bool IsDuplicateTransfer(string from, string to, IReadOnlyList<CardTransferCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return false;
            }

            var key = BuildTransferKey(from, to, cards);
            var now = DateTime.UtcNow;
            if (key == _lastTransferKey && (now - _lastTransferAt).TotalMilliseconds < 2000)
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
