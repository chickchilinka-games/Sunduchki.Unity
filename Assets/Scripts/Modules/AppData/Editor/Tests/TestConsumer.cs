using Modules.AppData.Interfaces;

namespace Modules.AppData.Editor.Tests
{
    internal sealed class TestConsumer : IAppDataConsumer
    {
        public string ModuleName { get; }
        public string LastSerializedPayload { get; private set; }

        public TestConsumer(string moduleName) => ModuleName = moduleName;

        public void SetData(string serializedData, ISerializer serializer)
        {
            LastSerializedPayload = serializedData;
        }
    }

}