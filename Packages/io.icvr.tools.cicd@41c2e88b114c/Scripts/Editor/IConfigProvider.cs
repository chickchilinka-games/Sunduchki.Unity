

namespace ICVR.Tools
{
    public interface IConfigProvider
    {
        TData GetData<TData>() where TData : class;
        string GetRawData();
        bool CreateFileFromProperty(string propertyName, string resultFilePath);
    }
}