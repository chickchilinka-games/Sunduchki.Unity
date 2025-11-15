using System.Collections.Generic;
using Modules.PlayerHand.Data;
using R3;

namespace Modules.PlayerHand.Model
{
    public class PlayerHandModel
    {
        private readonly Dictionary<string, ReactiveProperty<PlayerHandState>> _hands = new();

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
    }
}
