using System;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.DeckSystem.Model;
using R3;

namespace Modules.DeckSystem.Services
{
    public class DeckService : IDeckService, IDeckStateWriter
    {
        private readonly DeckModel _model;

        public DeckService(DeckModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<DeckState> State => _model.State;

        public DeckState Current => _model.Current;

        public void ConfigureCounts(int? remaining, int? total)
        {
            var current = _model.Current;
            var nextRemaining = remaining ?? current.RemainingCards;
            var nextTotal = total ?? current.TotalCards;
            _model.SetState(current.WithCounts(nextRemaining, nextTotal));
        }

        public void AdjustBy(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            var current = _model.Current;
            if (!current.RemainingCards.HasValue)
            {
                return;
            }

            var next = current.RemainingCards.Value + delta;
            if (next < 0)
            {
                next = 0;
            }

            _model.SetState(current.WithRemaining(next));
        }

        public void SetPeek(DeckPeekInfo info)
        {
            _model.SetState(_model.Current.WithPeek(info));
        }

        public void ClearPeek()
        {
            _model.SetState(_model.Current.WithPeek(DeckPeekInfo.None));
        }

        public void Reset()
        {
            _model.SetState(DeckState.Default);
        }
    }
}
