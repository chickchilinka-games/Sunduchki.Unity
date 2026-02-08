using System;
using UnityEngine;

namespace Modules.AuthenticationSystem.Config
{
    [CreateAssetMenu(fileName = "AuthApiConfigProvider", menuName = "Sunduchki/Authentication/AuthApiConfigProvider")]
    public class AuthApiConfigProviderAsset : ScriptableObject, IAuthApiConfigProvider
    {
        [SerializeField]
        private BaseUrlConfigAsset _baseConfig;

        [SerializeField, HideInInspector]
        private string _baseAddress = "http://localhost:5092/accounts";

        public AuthApiConfig GetConfig()
        {
            var baseUri = ResolveBaseUri();
            return new AuthApiConfig(baseUri);
        }

        private Uri ResolveBaseUri()
        {
            if (_baseConfig != null)
            {
                return _baseConfig.GetAccountsBaseUri();
            }

            if (!string.IsNullOrWhiteSpace(_baseAddress) && Uri.TryCreate(_baseAddress, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }

            return new Uri("http://localhost:5092/accounts");
        }
    }
}
