namespace Modules.PlayerData.Interfaces
{
    public interface IPlayerDataConsumer
    {
        string ModuleName { get; }
        void SetData(string serializedData, ISerializer serializer);
        string GetSerializedData(ISerializer serializer);
    }
}