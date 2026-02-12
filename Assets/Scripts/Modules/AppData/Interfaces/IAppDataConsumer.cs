namespace Modules.AppData.Interfaces
{
    public interface IAppDataConsumer
    {
        string ModuleName { get; }
        void SetData(string serializedData, ISerializer serializer);
    }
}
