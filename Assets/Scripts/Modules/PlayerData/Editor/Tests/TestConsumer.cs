using Modules.PlayerData.Interfaces;

namespace Modules.PlayerData.Editor.Tests
{
    internal sealed class TestConsumer : IPlayerDataConsumer
    {
        public string ModuleName { get; }
        public string AppliedPayload { get; private set; }
        public string NextSerialized { get; set; } = "{}";

        public TestConsumer(string name) => ModuleName = name;
        public void SetData(string serializedData, ISerializer serializer) => AppliedPayload = serializedData;
        public string GetSerializedData(ISerializer serializer) => NextSerialized;
    }
}