using System.Collections.Generic;
using Modules.PlayerData.Interfaces;

namespace Modules.PlayerData
{
    public class PlayerDataConsumersCollector : IPlayerDataConsumersCollector
    {
        private readonly IEnumerable<IPlayerDataConsumer> _consumers;

        public PlayerDataConsumersCollector(IEnumerable<IPlayerDataConsumer> consumers)
        {
            _consumers = consumers;
        }

        public IEnumerable<IPlayerDataConsumer> CollectImplementations() => _consumers;
    }
}
