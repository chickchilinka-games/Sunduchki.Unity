using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Storage;
using Modules.CardRequestSystem.Data;
using Modules.PlayerHand.Data;
using R3;

namespace Features.PlayerHandSystemImpl.Presenters
{
    public sealed class CardRequestPresentationPresenter : IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly Subject<CardRequestEvent> _cardRequested = new();
        private readonly Subject<CardRequestEvent> _cardTransferred = new();
        private readonly Subject<CardsReceivedEvent> _cardsReceived = new();

        private int _lastCardRequestSequence = -1;
        private bool _awaitingAskResponse;
        private CancellationTokenSource _awaitingAskCts;
        private DateTime _lastAskResolvedAt = DateTime.MinValue;

        public CardRequestPresentationPresenter(PlayerHandPresentationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Observable<CardRequestEvent> CardRequested => _cardRequested;
        public Observable<CardRequestEvent> CardTransferred => _cardTransferred;
        public Observable<CardsReceivedEvent> CardsReceived => _cardsReceived;

        public void ResetSession()
        {
            _lastCardRequestSequence = -1;
            _awaitingAskResponse = false;
            _context.SetAwaitingAsk(false);
            _awaitingAskCts?.Cancel();
            _awaitingAskCts?.Dispose();
            _awaitingAskCts = null;
            _lastAskResolvedAt = DateTime.MinValue;
        }

        public void Dispose()
        {
            ResetSession();
            _cardRequested.Dispose();
            _cardTransferred.Dispose();
            _cardsReceived.Dispose();
        }

        public void OnCardRequestStateChanged(CardRequestState state)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            if (!state.HasEvent || state.Sequence == _lastCardRequestSequence)
            {
                return;
            }

            _lastCardRequestSequence = state.Sequence;
            var evt = state.LastEvent;
            UpdatePendingAskState(evt);
            switch (evt.EventType)
            {
                case CardRequestEventType.Requested:
                    _cardRequested.OnNext(evt);
                    break;
                case CardRequestEventType.Transferred:
                    _cardTransferred.OnNext(evt);
                    break;
            }
        }

        public void OnCardsReceivedEvent(CardsReceivedEvent evt)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            var localId = _context.LocalPlayerId.CurrentValue;
            if (string.IsNullOrWhiteSpace(localId))
            {
                return;
            }

            if (!string.Equals(evt.PlayerId, localId, StringComparison.Ordinal))
            {
                return;
            }

            if (_awaitingAskResponse)
            {
                ClearAwaitingAsk();
            }

            MarkAskResolved();
            _cardsReceived.OnNext(evt);
        }

        private void UpdatePendingAskState(CardRequestEvent evt)
        {
            var localId = _context.LocalPlayerId.CurrentValue;
            if (string.IsNullOrWhiteSpace(localId))
            {
                return;
            }

            if (evt.EventType == CardRequestEventType.Requested)
            {
                if (string.Equals(evt.AskerId, localId, StringComparison.Ordinal))
                {
                    SetAwaitingAsk();
                }
                return;
            }

            if (evt.EventType == CardRequestEventType.Denied)
            {
                if (string.Equals(evt.AskerId, localId, StringComparison.Ordinal))
                {
                    ClearAwaitingAsk();
                    MarkAskResolved();
                }
                return;
            }

            if (evt.EventType == CardRequestEventType.Transferred)
            {
                if (string.Equals(evt.TargetId, localId, StringComparison.Ordinal))
                {
                    ClearAwaitingAsk();
                    MarkAskResolved();
                }
            }
        }

        private void SetAwaitingAsk()
        {
            var now = DateTime.UtcNow;
            if (now - _lastAskResolvedAt < TimeSpan.FromMilliseconds(500))
            {
                return;
            }

            _awaitingAskResponse = true;
            _context.SetAwaitingAsk(true);

            _awaitingAskCts?.Cancel();
            _awaitingAskCts?.Dispose();
            _awaitingAskCts = new CancellationTokenSource();
            AutoClearAwaitingAsync(_awaitingAskCts.Token).Forget();
        }

        private void ClearAwaitingAsk()
        {
            if (!_awaitingAskResponse)
            {
                return;
            }

            _awaitingAskResponse = false;
            _context.SetAwaitingAsk(false);
            _awaitingAskCts?.Cancel();
            _awaitingAskCts?.Dispose();
            _awaitingAskCts = null;
        }

        private void MarkAskResolved()
        {
            _lastAskResolvedAt = DateTime.UtcNow;
        }

        private async UniTaskVoid AutoClearAwaitingAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (_awaitingAskResponse)
            {
                _awaitingAskResponse = false;
                _context.SetAwaitingAsk(false);
            }
        }
    }
}
