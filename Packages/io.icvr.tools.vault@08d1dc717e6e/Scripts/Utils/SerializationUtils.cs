// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

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