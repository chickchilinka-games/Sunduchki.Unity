using System.Collections.Generic;

namespace Modules.AppData.Interfaces
{
    public interface IAppDataConsumersCollector
    {
        IEnumerable<IAppDataConsumer> CollectImplementations();
    }
}