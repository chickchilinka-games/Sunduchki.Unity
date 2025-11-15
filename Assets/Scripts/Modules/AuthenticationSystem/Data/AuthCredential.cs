using System;
using System.Collections.Generic;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public sealed class AuthCredential
    {
        private readonly Dictionary<string, object> _parameters;

        public AuthCredential(AuthType authType, string username, Dictionary<string, object> parameters)
        {
            AuthType = authType;
            Username = username;
            _parameters = parameters != null
                ? new Dictionary<string, object>(parameters)
                : new Dictionary<string, object>();
        }

        public AuthType AuthType { get; }

        public string Username { get; }

        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        public bool TryGet<T>(string key, out T value)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));

            if (_parameters.TryGetValue(key, out var rawValue))
            {
                if (rawValue is not T typedValue)
                {
                    value = default;
                    return false;
                }
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}
