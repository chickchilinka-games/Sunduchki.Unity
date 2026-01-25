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
            _stateWriter.RegisterTransfer(from, to, cards ?? Array.Empty<CardTransferCardData>());
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
    }
}
