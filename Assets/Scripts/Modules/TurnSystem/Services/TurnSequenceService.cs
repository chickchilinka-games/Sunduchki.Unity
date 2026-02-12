using System;
using Modules.TurnSystem.Data;
using Modules.TurnSystem.Model;
using R3;

namespace Modules.TurnSystem.Services
{
    public class TurnSequenceService
    {
        private readonly TurnSequenceModel _model;

        public TurnSequenceService(TurnSequenceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            CurrentPlayerStream = _model.State.Select(state => state.CurrentPlayerId).DistinctUntilChanged();
        }

        public ReadOnlyReactiveProperty<TurnState> State => _model.State;

        public Observable<string> CurrentPlayerStream { get; }
    }
}
