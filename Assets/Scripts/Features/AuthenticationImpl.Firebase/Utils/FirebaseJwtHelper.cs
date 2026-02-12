using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Modules.AuthenticationSystem.Utils
{
    public static class FirebaseJwtHelper
    {
        public static string ExtractRole(string jwt)
        {
            var parts = jwt.Split('.');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));
            var json = JObject.Parse(payload);
            if (json.TryGetValue("role", out var role)) return role.ToString();
            if (json.TryGetValue("roles", out var roles) && roles is JArray { Count: > 0 } arr)
                return arr[0].ToString();
            return "user";
        }

        private static string PadBase64(string s)
        {
            int pad = 4 - (s.Length % 4);
            if (pad < 4) s = s + new string('=', pad);
            return s.Replace('-', '+').Replace('_', '/');
        }
    }
}
