using System.Collections.Generic;
using Modules.PlayerHand.Data;
using R3;

namespace Modules.PlayerHand.Model
{
    public class PlayerHandModel
    {
        private readonly Dictionary<string, ReactiveProperty<PlayerHandState>> _hands = new();
        private readonly Subject<BonusCardUsageEvent> _bonusUsed = new();
        private readonly Subject<StandardCardDrawnEvent> _standardCardDrawn = new();
        private readonly Subject<BonusCardDrawnEvent> _bonusCardDrawn = new();
        private readonly Subject<CardsReceivedEvent> _cardsReceived = new();

        public Observable<BonusCardUsageEvent> BonusUsed => _bonusUsed;
        public Observable<StandardCardDrawnEvent> StandardCardDrawn => _standardCardDrawn;
        public Observable<BonusCardDrawnEvent> BonusCardDrawn => _bonusCardDrawn;
        public Observable<CardsReceivedEvent> CardsReceived => _cardsReceived;

        public ReactiveProperty<PlayerHandState> GetOrCreate(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = string.Empty;
            }

            if (_hands.TryGetValue(playerId, out var property))
            {
                return property;
            }

            var reactive = new ReactiveProperty<PlayerHandState>(PlayerHandState.Empty(playerId));
            _hands[playerId] = reactive;
            return reactive;
        }

        public void ResetAll()
        {
            foreach (var pair in _hands)
            {
                pair.Value.Value = PlayerHandState.Empty(pair.Key);
            }
        }

        internal void PublishBonusUsed(BonusCardUsageEvent evt)
        {
            _bonusUsed.OnNext(evt);
        }

        internal void PublishStandardCardDrawn(StandardCardDrawnEvent evt)
        {
            _standardCardDrawn.OnNext(evt);
        }

        internal void PublishBonusCardDrawn(BonusCardDrawnEvent evt)
        {
            _bonusCardDrawn.OnNext(evt);
        }

        internal void PublishCardsReceived(CardsReceivedEvent evt)
        {
            _cardsReceived.OnNext(evt);
        }
    }
}
