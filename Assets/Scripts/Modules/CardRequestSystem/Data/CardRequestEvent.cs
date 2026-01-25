using System;
using System.Collections.Generic;

namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestEvent
    {
        public CardRequestEventType EventType { get; }
        public string AskerId { get; }
        public string TargetId { get; }
        public string Rank { get; }
        public int Count { get; }
        public IReadOnlyList<CardTransferCardData> Cards { get; }
        public DateTime Timestamp { get; }

        public CardRequestEvent(
            CardRequestEventType eventType,
            string askerId,
            string targetId,
            string rank,
            int count,
            IReadOnlyList<CardTransferCardData> cards,
            DateTime timestamp)
        {
            EventType = eventType;
            AskerId = askerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Rank = rank ?? string.Empty;
            Count = count;
            Cards = cards ?? Array.Empty<CardTransferCardData>();
            Timestamp = timestamp;
        }
    }
}
