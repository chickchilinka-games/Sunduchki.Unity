using System.Collections.Generic;

namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestTransferredEvent
    {
        public string ActionId { get; }
        public string FromPlayerId { get; }
        public string TargetPlayerId { get; }
        public IReadOnlyList<CardTransferCardData> Cards { get; }

        public CardRequestTransferredEvent(
            string actionId,
            string fromPlayerId,
            string targetPlayerId,
            IReadOnlyList<CardTransferCardData> cards)
        {
            ActionId = actionId ?? string.Empty;
            FromPlayerId = fromPlayerId ?? string.Empty;
            TargetPlayerId = targetPlayerId ?? string.Empty;
            Cards = cards ?? new List<CardTransferCardData>();
        }
    }
}
