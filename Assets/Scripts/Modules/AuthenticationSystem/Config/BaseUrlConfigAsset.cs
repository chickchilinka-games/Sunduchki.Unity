using System;
using UnityEngine;

namespace Modules.AuthenticationSystem.Config
{
    [CreateAssetMenu(fileName = "BaseUrlConfig", menuName = "Sunduchki/Network/BaseUrlConfig")]
    public class BaseUrlConfigAsset : ScriptableObject
    {
        [SerializeField]
        private string _accountsBaseUrl = "http://localhost:5000";

        [SerializeField]
        private string _gameBaseUrl = "http://localhost:5000";

        public Uri GetAccountsBaseUri()
        {
            if (Uri.TryCreate(_accountsBaseUrl, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }

            return new Uri("http://localhost:5000");
        }

        public Uri GetGameBaseUri()
        {
            if (Uri.TryCreate(_gameBaseUrl, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }

            return new Uri("http://localhost:5000");
        }
    }
}
