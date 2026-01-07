using System;
using Modules.Profiles.Data;
using UnityEngine;

namespace Modules.Profiles.Config
{
    [CreateAssetMenu(fileName = "ProfileApiConfigProvider", menuName = "Sunduchki/Profiles/ApiConfigProvider")]
    public class ProfileApiConfigProviderAsset : ScriptableObject, IProfileApiConfigProvider
    {
        [SerializeField]
        private string _baseAddress = "http://localhost:5092";

        public ProfileApiConfig GetConfig()
        {
            var uri = Uri.TryCreate(_baseAddress, UriKind.Absolute, out var parsed)
                ? parsed
                : new Uri("http://localhost:5092");

            return new ProfileApiConfig(uri);
        }
    }
}
