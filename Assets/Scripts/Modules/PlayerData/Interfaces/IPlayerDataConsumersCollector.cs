using System.Collections.Generic;

namespace Modules.PlayerData.Interfaces
{
    public interface IPlayerDataConsumersCollector
    {
        IEnumerable<IPlayerDataConsumer> CollectImplementations();
    }
}