using System;

namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestEvent
    {
        public CardRequestEventType EventType { get; }
        public string AskerId { get; }
        public string TargetId { get; }
        public string Rank { get; }
        public int Count { get; }
        public DateTime Timestamp { get; }

        public CardRequestEvent(
            CardRequestEventType eventType,
            string askerId,
            string targetId,
            string rank,
            int count,
            DateTime timestamp)
        {
            EventType = eventType;
            AskerId = askerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Rank = rank ?? string.Empty;
            Count = count;
            Timestamp = timestamp;
        }
    }
}
