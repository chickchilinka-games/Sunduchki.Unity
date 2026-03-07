using System.Collections.Generic;

namespace Features.PlayerHandSystemImpl.Presentation.Data
{
    public readonly struct RankCardsReceivedEvent
    {
        public string Source { get; }
        public IReadOnlyList<string> Suits { get; }
        public long EventSeq { get; }
        public bool CompletedSet { get; }

        public RankCardsReceivedEvent(
            string source,
            IReadOnlyList<string> suits,
            long eventSeq,
            bool completedSet)
        {
            Source = source ?? string.Empty;
            Suits = suits ?? new List<string>();
            EventSeq = eventSeq;
            CompletedSet = completedSet;
        }
    }
}
