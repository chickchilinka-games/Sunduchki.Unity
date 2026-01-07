using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.Profiles.Interfaces;
using UnityEngine;

namespace Modules.Profiles.Providers
{
    public sealed class FirebaseDisplayNameProvider : IDisplayNameProvider
    {
        public async UniTask<OperationResult> ChangeDisplayNameAsync(string displayName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult.Failed("Cancelled.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return OperationResult.Failed("Display name is required.");
            }

            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                return OperationResult.Failed("No active user session.");
            }

            try
            {
                var profile = new UserProfile { DisplayName = displayName };
                await user.UpdateUserProfileAsync(profile);
                if (cancellationToken.IsCancellationRequested)
                {
                    return OperationResult.Failed("Cancelled.");
                }

                await user.ReloadAsync();
                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Profiles] Failed to update display name: {ex.Message}");
                return OperationResult.Failed(ex.Message);
            }
        }
    }
}
