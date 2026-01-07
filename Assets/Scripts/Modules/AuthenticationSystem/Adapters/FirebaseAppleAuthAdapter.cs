using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Adapters
{
    public sealed class FirebaseAppleAuthAdapter : IFirebaseAuthAdapter
    {
        public AuthType AuthType => AuthType.Apple;
        public string ProviderId => "apple.com";

        public async UniTask<FirebaseUser> SignInAsync(AuthCredential credential, FirebaseAuth auth)
        {
            var appleCredential = AuthCredentialBuilder.ParseApple(credential);

            if (TryCreateCredential(appleCredential, out var firebaseCred))
            {
                var user = await auth.SignInWithCredentialAsync(firebaseCred);
                return user;
            }

            var provider = CreateFederatedProvider(appleCredential);
            var res2 = await auth.SignInWithProviderAsync(provider);
            return res2.User;
        }

        public async UniTask<FirebaseUser> LinkAsync(AuthCredential credential, FirebaseUser currentUser,
            FirebaseAuth auth)
        {
            if (currentUser == null)
                throw new InvalidOperationException("No active user to link.");

            var appleCredential = AuthCredentialBuilder.ParseApple(credential);

            if (TryCreateCredential(appleCredential, out var firebaseCred))
            {
                var res = await currentUser.LinkWithCredentialAsync(firebaseCred);
                return res.User;
            }

            var provider = CreateFederatedProvider(appleCredential);
            var res2 = await currentUser.LinkWithProviderAsync(provider);
            return res2?.User ?? currentUser;
        }

        public async UniTask ReauthenticateAsync(AuthCredential credential, FirebaseUser user, FirebaseAuth auth)
        {
            var appleCredential = AuthCredentialBuilder.ParseApple(credential);

            if (TryCreateCredential(appleCredential, out var firebaseCred))
            {
                await user.ReauthenticateAsync(firebaseCred);
                return;
            }

            var provider = CreateFederatedProvider(appleCredential);
            await user.ReauthenticateWithProviderAsync(provider);
        }

        private static bool TryCreateCredential(AppleAuthCredential credential, out Credential firebaseCredential)
        {
            firebaseCredential = null;

            if (!string.IsNullOrEmpty(credential.IdToken) && !string.IsNullOrEmpty(credential.RawNonce))
            {
                firebaseCredential = OAuthProvider.GetCredential("apple.com", credential.IdToken, credential.RawNonce, credential.AccessToken);
                return true;
            }

            return false;
        }

        private static FederatedOAuthProvider CreateFederatedProvider(AppleAuthCredential credential)
        {
            var data = new FederatedOAuthProviderData { ProviderId = "apple.com" };

            data.Scopes = credential.Scopes.Any() ? credential.Scopes.ToArray() : new[] { "name", "email" };

            if (credential.CustomParameters.Count > 0)
            {
                data.CustomParameters = new Dictionary<string, string>(credential.CustomParameters);
            }

            var provider = new FederatedOAuthProvider();
            provider.SetProviderData(data);
            return provider;
        }
    }
}
