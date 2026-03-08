namespace Features.PlayerHandSystemImpl.View
{
    public partial class PlayerHandHolder
    {
        private bool IsTransferredRank(string rank)
        {
            return _animationGate != null && _animationGate.IsTransferredRank(rank);
        }

        private void ClearTransferredRank(string rank)
        {
            _animationGate?.ClearTransferredRank(rank);
        }

        private bool IsTransferOutInProgress(string rank)
        {
            return _animationGate != null && _animationGate.IsTransferOutInProgress(rank);
        }

        private void ClearTransferOutInProgress(string rank)
        {
            _animationGate?.ClearTransferOutInProgress(rank);
        }

        private bool ShouldWaitTransferOutStart(string rank)
        {
            return _animationGate != null && _animationGate.ShouldWaitTransferOutStart(rank);
        }

        private void ClearRemovalStartGrace(string rank)
        {
            _animationGate?.ClearRemovalStartGrace(rank);
        }


        private bool IsPendingSetCompletion(string rank)
        {
            return _animationGate != null && _animationGate.IsPendingSetCompletion(rank);
        }

        private void MarkPendingSetCompletion(string rank)
        {
            _animationGate?.MarkPendingSetCompletion(rank);
        }

        private void ClearPendingSetCompletion(string rank)
        {
            _animationGate?.ClearPendingSetCompletion(rank);
        }

        public void MarkRankTransferredOut(string rank)
        {
            _animationGate?.MarkRankTransferredOut(rank);
        }

        public void CompleteRankTransfer(string rank)
        {
            _animationGate?.CompleteRankTransfer(rank);
            RequestLayoutRefresh();
        }
    }
}

