using System;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Model;
using R3;

namespace Modules.DeckSystem.Services
{
    public class DeckService
    {
        private readonly DeckModel _model;

        public DeckService(DeckModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<DeckState> State => _model.State;
        public Observable<DeckConfiguredEvent> Configured => _model.Configured;
        public Observable<DeckCardDrawnEvent> StandardDrawn => _model.StandardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _model.BonusDrawn;
        public Observable<DeckPeekedEvent> Peeked => _model.Peeked;
        public Observable<DeckAdjustedEvent> Adjusted => _model.Adjusted;

        public DeckState Current => _model.Current;
    }
}
