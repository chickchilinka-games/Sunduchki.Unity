using System;
using Modules.TurnSystem.Data;
using Modules.TurnSystem.Model;

namespace Modules.TurnSystem.Services
{
    internal class TurnSequenceInternalService
    {
        private readonly TurnSequenceModel _model;
        private string _localPlayerId;

        public TurnSequenceInternalService(TurnSequenceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void SetLocalPlayer(string playerId)
        {
            _localPlayerId = playerId ?? string.Empty;
            var current = _model.Current;
            _model.SetState(new TurnState(
                current.CurrentPlayerId,
                current.PreviousPlayerId,
                current.TurnIndex,
                IsLocal(current.CurrentPlayerId)));
        }

        public void Reset()
        {
            _model.SetState(TurnState.Default);
        }

        public void AdvanceTo(string playerId)
        {
            var nextId = playerId ?? string.Empty;
            var nextState = _model.Current.WithCurrent(nextId, IsLocal(nextId));
            _model.SetState(nextState);
        }

        private bool IsLocal(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) &&
                   !string.IsNullOrWhiteSpace(_localPlayerId) &&
                   string.Equals(playerId, _localPlayerId, StringComparison.Ordinal);
        }
    }
}
