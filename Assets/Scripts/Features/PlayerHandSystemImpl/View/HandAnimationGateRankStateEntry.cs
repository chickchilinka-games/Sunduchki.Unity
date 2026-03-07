namespace Features.PlayerHandSystemImpl.View
{
    internal enum HandRankLifecycleState
    {
        Idle,
        Receiving,
        TransferOut,
        SetCompleting,
        Removing
    }

    internal sealed class HandAnimationGateRankStateEntry
    {
        public HandRankLifecycleState State = HandRankLifecycleState.Idle;
        public bool TransferMarked;
        public int ActiveTransferCount;
        public float TransferReleaseUntil;
        public float RemovalGraceUntil;
        public bool RemovalGracePending;
    }
}
