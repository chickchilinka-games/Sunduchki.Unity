using System;
using Modules.AuthenticationSystem.Config;
using Modules.Profiles.Data;
using UnityEngine;

namespace Modules.Profiles.Config
{
    [CreateAssetMenu(fileName = "ProfileApiConfigProvider", menuName = "Sunduchki/Profiles/ApiConfigProvider")]
    public class ProfileApiConfigProviderAsset : ScriptableObject, IProfileApiConfigProvider
    {
        [SerializeField]
        private BaseUrlConfigAsset _baseConfig;

        [SerializeField, HideInInspector]
        private string _baseAddress = "http://localhost:5092/accounts";

        public ProfileApiConfig GetConfig()
        {
            var baseUri = ResolveBaseUri();
            return new ProfileApiConfig(baseUri);
        }

        private Uri ResolveBaseUri()
        {
            if (_baseConfig != null)
            {
                return _baseConfig.GetAccountsBaseUri();
            }

            if (Uri.TryCreate(_baseAddress, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }

            return new Uri("http://localhost:5092/accounts");
        }
    }
}
