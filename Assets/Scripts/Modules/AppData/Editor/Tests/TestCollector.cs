using System.Collections.Generic;
using Modules.AppData.Interfaces;

namespace Modules.AppData.Editor.Tests
{
    internal sealed class TestCollector : IAppDataConsumersCollector
    {
        private readonly IEnumerable<IAppDataConsumer> _consumers;
        public TestCollector(params IAppDataConsumer[] consumers) => _consumers = consumers;
        public IEnumerable<IAppDataConsumer> CollectImplementations() => _consumers;
    }

}