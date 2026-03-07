using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    internal sealed class HandAnimationGate
    {
        private readonly Dictionary<string, HandAnimationGateRankStateEntry> _rankStates =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly float _transferOutReleaseDelaySeconds;
        private readonly float _removalStartGraceSeconds;

        public HandAnimationGate(float transferOutReleaseDelaySeconds, float removalStartGraceSeconds)
        {
            _transferOutReleaseDelaySeconds = transferOutReleaseDelaySeconds;
            _removalStartGraceSeconds = removalStartGraceSeconds;
        }

        public void Clear()
        {
            _rankStates.Clear();
        }

        public bool IsTransferredRank(string rank)
        {
            return TryGet(rank, out _, out var entry) && entry.TransferMarked;
        }

        public void ClearTransferredRank(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            entry.TransferMarked = false;
            RefreshState(rankKey, entry);
        }

        public bool IsTransferOutInProgress(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return false;
            }

            CleanupTimers(entry);
            RefreshState(rankKey, entry);
            var now = Time.unscaledTime;
            return entry.ActiveTransferCount > 0 || now < entry.TransferReleaseUntil;
        }

        public void ClearTransferOutInProgress(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            entry.ActiveTransferCount = 0;
            entry.TransferReleaseUntil = 0f;
            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            RefreshState(rankKey, entry);
        }

        public bool ShouldWaitTransferOutStart(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return false;
            }

            var entry = GetOrCreate(rankKey);
            CleanupTimers(entry);

            if (entry.TransferMarked || IsTransferOutInProgress(rankKey) || IsPendingSetCompletion(rankKey))
            {
                ClearRemovalStartGrace(rankKey);
                return false;
            }

            if (entry.RemovalGraceUntil <= 0f)
            {
                entry.RemovalGraceUntil = Time.unscaledTime + _removalStartGraceSeconds;
            }

            if (Time.unscaledTime < entry.RemovalGraceUntil)
            {
                entry.RemovalGracePending = true;
                return true;
            }

            ClearRemovalStartGrace(rankKey);
            return false;
        }

        public void ClearRemovalStartGrace(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            RefreshState(rankKey, entry);
        }

        public void MarkRemovalStartGracePending(string rank)
        {
            if (!TryGet(rank, out _, out var entry))
            {
                return;
            }

            entry.RemovalGracePending = true;
        }

        public void ClearRemovalStartGracePending(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            entry.RemovalGracePending = false;
            RefreshState(rankKey, entry);
        }

        public bool HasBlockingRankState()
        {
            if (_rankStates.Count == 0)
            {
                return false;
            }

            var now = Time.unscaledTime;
            var keys = _rankStates.Keys.ToArray();
            foreach (var rank in keys)
            {
                if (!_rankStates.TryGetValue(rank, out var entry))
                {
                    continue;
                }

                CleanupTimers(entry);
                RefreshState(rank, entry);

                if (entry.RemovalGracePending ||
                    entry.State == HandRankLifecycleState.TransferOut ||
                    entry.State == HandRankLifecycleState.SetCompleting ||
                    entry.State == HandRankLifecycleState.Removing ||
                    entry.ActiveTransferCount > 0 ||
                    now < entry.TransferReleaseUntil)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPendingSetCompletion(string rank)
        {
            return TryGet(rank, out _, out var entry) &&
                   entry.State == HandRankLifecycleState.SetCompleting;
        }

        public void MarkPendingSetCompletion(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var entry = GetOrCreate(rankKey);
            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            entry.State = HandRankLifecycleState.SetCompleting;
        }

        public void ClearPendingSetCompletion(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            if (entry.State == HandRankLifecycleState.SetCompleting)
            {
                entry.State = HandRankLifecycleState.Idle;
            }

            RefreshState(rankKey, entry);
        }

        public void MarkRankTransferredOut(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var entry = GetOrCreate(rankKey);
            entry.TransferMarked = true;
            entry.ActiveTransferCount++;
            entry.TransferReleaseUntil = 0f;
            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            entry.State = HandRankLifecycleState.TransferOut;
        }

        public void CompleteRankTransfer(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            if (entry.ActiveTransferCount > 0)
            {
                entry.ActiveTransferCount--;
            }

            if (entry.ActiveTransferCount <= 0)
            {
                entry.ActiveTransferCount = 0;
                entry.TransferReleaseUntil = Time.unscaledTime + _transferOutReleaseDelaySeconds;
            }

            RefreshState(rankKey, entry);
        }

        public void MarkReceiving(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var entry = GetOrCreate(rankKey);
            if (entry.State == HandRankLifecycleState.Idle)
            {
                entry.State = HandRankLifecycleState.Receiving;
            }
        }

        public void CompleteReceiving(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            if (entry.State == HandRankLifecycleState.Receiving)
            {
                entry.State = HandRankLifecycleState.Idle;
            }

            RefreshState(rankKey, entry);
        }

        public void MarkRemoving(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var entry = GetOrCreate(rankKey);
            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            entry.State = HandRankLifecycleState.Removing;
        }

        public void FinalizeRemoval(string rank)
        {
            if (!TryGet(rank, out var rankKey, out var entry))
            {
                return;
            }

            entry.TransferMarked = false;
            entry.ActiveTransferCount = 0;
            entry.TransferReleaseUntil = 0f;
            entry.RemovalGraceUntil = 0f;
            entry.RemovalGracePending = false;
            entry.State = HandRankLifecycleState.Idle;
            RefreshState(rankKey, entry);
        }

        private void CleanupTimers(HandAnimationGateRankStateEntry entry)
        {
            var now = Time.unscaledTime;
            if (entry.TransferReleaseUntil > 0f && now >= entry.TransferReleaseUntil)
            {
                entry.TransferReleaseUntil = 0f;
            }

            if (entry.RemovalGraceUntil > 0f && now >= entry.RemovalGraceUntil)
            {
                entry.RemovalGraceUntil = 0f;
                entry.RemovalGracePending = false;
            }
        }

        private void RefreshState(string rankKey, HandAnimationGateRankStateEntry entry)
        {
            CleanupTimers(entry);
            var now = Time.unscaledTime;
            var transferActive = entry.TransferMarked || entry.ActiveTransferCount > 0 || now < entry.TransferReleaseUntil;
            if (entry.State != HandRankLifecycleState.Removing &&
                entry.State != HandRankLifecycleState.SetCompleting &&
                entry.State != HandRankLifecycleState.Receiving)
            {
                entry.State = transferActive
                    ? HandRankLifecycleState.TransferOut
                    : HandRankLifecycleState.Idle;
            }

            if (!entry.TransferMarked &&
                entry.ActiveTransferCount <= 0 &&
                entry.TransferReleaseUntil <= 0f &&
                !entry.RemovalGracePending &&
                entry.State == HandRankLifecycleState.Idle)
            {
                _rankStates.Remove(rankKey);
            }
        }

        private bool TryGet(string rank, out string rankKey, out HandAnimationGateRankStateEntry entry)
        {
            rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                entry = null;
                return false;
            }

            return _rankStates.TryGetValue(rankKey, out entry);
        }

        private HandAnimationGateRankStateEntry GetOrCreate(string rankKey)
        {
            if (_rankStates.TryGetValue(rankKey, out var entry))
            {
                return entry;
            }

            entry = new HandAnimationGateRankStateEntry();
            _rankStates[rankKey] = entry;
            return entry;
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }
    }
}
