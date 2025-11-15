namespace Modules.PlayerData.Interfaces
{
    public interface ISerializer
    {
        string Serialize<T>(T data);
        bool TryDeserialize<T>(string serializedData, out T data);
    }
}