using System;
using System.Collections.Generic;

namespace Features.PlayerHandSystemImpl.Presentation.Data
{
    internal enum RankStackCommandType
    {
        ApplySnapshot,
        Receive,
        TransferOut,
        SetComplete
    }

    internal readonly struct RankStackCommand
    {
        public RankStackCommandType Type { get; }
        public IReadOnlyList<string> Suits { get; }
        public string Source { get; }
        public long EventSeq { get; }
        public bool CompletedSet { get; }
        public int Count { get; }
        public string ActionId { get; }
        public long Revision { get; }

        private RankStackCommand(
            RankStackCommandType type,
            IReadOnlyList<string> suits,
            string source,
            long eventSeq,
            bool completedSet,
            int count,
            string actionId,
            long revision)
        {
            Type = type;
            Suits = suits ?? Array.Empty<string>();
            Source = source ?? string.Empty;
            EventSeq = eventSeq;
            CompletedSet = completedSet;
            Count = count;
            ActionId = actionId ?? string.Empty;
            Revision = revision;
        }

        public static RankStackCommand Snapshot(IReadOnlyList<string> suits, long revision)
        {
            return new RankStackCommand(
                RankStackCommandType.ApplySnapshot,
                suits,
                string.Empty,
                0,
                false,
                0,
                string.Empty,
                revision);
        }

        public static RankStackCommand Receive(string source, IReadOnlyList<string> suits, long eventSeq, bool completedSet)
        {
            return new RankStackCommand(
                RankStackCommandType.Receive,
                suits,
                source,
                eventSeq,
                completedSet,
                0,
                string.Empty,
                0);
        }

        public static RankStackCommand TransferOut(IReadOnlyList<string> suits, int count, string actionId)
        {
            return new RankStackCommand(
                RankStackCommandType.TransferOut,
                suits,
                string.Empty,
                0,
                false,
                count,
                actionId,
                0);
        }

        public static RankStackCommand SetComplete(long eventSeq = 0)
        {
            return new RankStackCommand(
                RankStackCommandType.SetComplete,
                Array.Empty<string>(),
                string.Empty,
                eventSeq,
                true,
                0,
                string.Empty,
                0);
        }
    }
}
