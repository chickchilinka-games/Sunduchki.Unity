using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICVR.Tools.Vault.Utils
{
    internal class SerializationUtils
    {
        public static string Serialize<TData>(TData data) where TData : class
        {
            return JsonConvert.SerializeObject(data);
        }

        public static T GetFieldValue<T>(string serializedData, string field)
        {
            var jObject = JObject.Parse(serializedData);
            return jObject[field].Value<T>();
        }

        public static TData Deserialize<TData>(string serializedData) where TData : class
        {
            return JsonConvert.DeserializeObject<TData>(serializedData);
        }
    }
}