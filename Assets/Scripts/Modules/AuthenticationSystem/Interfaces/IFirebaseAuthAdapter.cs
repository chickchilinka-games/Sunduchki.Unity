using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;

namespace Modules.AuthenticationSystem.Interfaces
{
    public interface IFirebaseAuthAdapter
    {
        AuthType AuthType { get; }
        string ProviderId { get; }

        UniTask<FirebaseUser> SignInAsync(AuthCredential credential, FirebaseAuth auth);

        UniTask<FirebaseUser> LinkAsync(AuthCredential credential, FirebaseUser currentUser, FirebaseAuth auth);

        UniTask ReauthenticateAsync(AuthCredential credential, FirebaseUser user, FirebaseAuth auth);
    }
}