using System;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Model;
using R3;

namespace Modules.PlayerHand.Services
{
    public class PlayerHandService
    {
        private readonly PlayerHandModel _model;

        public PlayerHandService(PlayerHandModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<PlayerHandState> ObserveHand(string playerId)
        {
            return _model.GetOrCreate(playerId);
        }

        public Observable<BonusCardUsageEvent> BonusUsed => _model.BonusUsed;
        public Observable<StandardCardDrawnEvent> StandardCardDrawn => _model.StandardCardDrawn;
        public Observable<BonusCardDrawnEvent> BonusCardDrawn => _model.BonusCardDrawn;
        public Observable<CardsReceivedEvent> CardsReceived => _model.CardsReceived;
    }
}
