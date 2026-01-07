using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Adapters
{
    public sealed class FirebaseGoogleAuthAdapter : IFirebaseAuthAdapter
    {
        public AuthType AuthType => AuthType.Google;
        public string ProviderId => "google.com";

        public async UniTask<FirebaseUser> SignInAsync(AuthCredential credential, FirebaseAuth auth)
        {
            var googleCredential = AuthCredentialBuilder.ParseGoogle(credential);

            if (TryCreateCredential(googleCredential, out var firebaseCred))
            {
                var user = await auth.SignInWithCredentialAsync(firebaseCred);
                return user;
            }

            var provider = CreateFederatedProvider(googleCredential);
            var res2 = await auth.SignInWithProviderAsync(provider);
            return res2.User;
        }

        public async UniTask<FirebaseUser> LinkAsync(AuthCredential credential, FirebaseUser currentUser,
            FirebaseAuth auth)
        {
            if (currentUser == null) throw new System.InvalidOperationException("No active user to link.");

            var googleCredential = AuthCredentialBuilder.ParseGoogle(credential);

            if (TryCreateCredential(googleCredential, out var firebaseCred))
            {
                var res = await currentUser.LinkWithCredentialAsync(firebaseCred);
                return res.User;
            }

            var provider = CreateFederatedProvider(googleCredential);
            var res2 = await currentUser.LinkWithProviderAsync(provider);
            return res2?.User ?? currentUser;
        }

        public async UniTask ReauthenticateAsync(AuthCredential credential, FirebaseUser user, FirebaseAuth auth)
        {
            var googleCredential = AuthCredentialBuilder.ParseGoogle(credential);

            if (TryCreateCredential(googleCredential, out var firebaseCred))
            {
                await user.ReauthenticateAsync(firebaseCred);
                return;
            }

            var provider = CreateFederatedProvider(googleCredential);
            await user.ReauthenticateWithProviderAsync(provider);
        }

        private static bool TryCreateCredential(GoogleAuthCredential credential, out Credential firebaseCredential)
        {
            firebaseCredential = null;

            if (!string.IsNullOrEmpty(credential.IdToken) || !string.IsNullOrEmpty(credential.AccessToken))
            {
                firebaseCredential = GoogleAuthProvider.GetCredential(credential.IdToken, credential.AccessToken);
                return true;
            }

            return false;
        }

        private static FederatedOAuthProvider CreateFederatedProvider(GoogleAuthCredential credential)
        {
            var data = new FederatedOAuthProviderData { ProviderId = "google.com" };

            data.Scopes = credential.Scopes.Any() ? credential.Scopes.ToArray() : new[] { "email" };
            data.CustomParameters = new Dictionary<string, string>(credential.CustomParameters);
            data.CustomParameters["prompt"] = "select_account";

            var provider = new FederatedOAuthProvider();
            provider.SetProviderData(data);
            return provider;
        }
    }
}
