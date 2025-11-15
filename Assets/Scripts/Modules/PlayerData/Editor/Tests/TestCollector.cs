using System.Collections.Generic;
using Modules.PlayerData.Interfaces;

namespace Modules.PlayerData.Editor.Tests
{
    internal sealed class TestCollector : IPlayerDataConsumersCollector
    {
        private readonly IPlayerDataConsumer[] _c;
        public TestCollector(params IPlayerDataConsumer[] c) => _c = c;
        public IEnumerable<IPlayerDataConsumer> CollectImplementations() => _c;
    }

}