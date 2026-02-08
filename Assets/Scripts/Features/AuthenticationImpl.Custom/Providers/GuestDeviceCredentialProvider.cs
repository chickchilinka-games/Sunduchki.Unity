using System;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;
using UnityEngine;

namespace Modules.AuthenticationSystem.Providers
{
    public sealed class GuestDeviceCredentialProvider : ICredentialProvider
    {
        private const string DeviceIdKey = "sunduchki.auth.deviceId";

        public AuthType AuthType => AuthType.Anonymous;

        public UniTask<AuthCredential> AcquireCredentialAsync()
        {
            var deviceId = ResolveDeviceId();
            var credential = AuthCredentialBuilder
                .For(AuthType)
                .WithParameter(AuthCredentialBuilder.ParameterKeys.DeviceId, deviceId)
                .Build();

            return UniTask.FromResult(credential);
        }

        private static string ResolveDeviceId()
        {
            if (PlayerPrefs.HasKey(DeviceIdKey))
            {
                var stored = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    return stored;
                }
            }

            var newId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(DeviceIdKey, newId);
            PlayerPrefs.Save();
            return newId;
        }
    }
}
