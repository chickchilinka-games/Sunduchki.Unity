using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;

namespace Modules.AuthenticationSystem.Adapters
{
    public sealed class FirebaseAnonymousAuthAdapter : IFirebaseAuthAdapter
    {
        public AuthType AuthType => AuthType.Anonymous;
        public string ProviderId => "anonymous";

        public async UniTask<FirebaseUser> SignInAsync(AuthCredential credential, FirebaseAuth auth)
        {
            var res = await auth.SignInAnonymouslyAsync();
            return res.User;
        }

        public UniTask<FirebaseUser> LinkAsync(AuthCredential credential, FirebaseUser currentUser, FirebaseAuth auth)
        {
            throw new NotSupportedException("Linking 'anonymous' provider is not supported.");
        }

        public UniTask ReauthenticateAsync(AuthCredential credential, FirebaseUser user, FirebaseAuth auth)
        {
            return UniTask.CompletedTask;
        }
    }
}