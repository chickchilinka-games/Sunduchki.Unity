using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Adapters
{
    public class FirebaseEmailAuthAdapter: IFirebaseAuthAdapter
    {
        public AuthType AuthType => AuthType.Email;
        public string ProviderId => "password";
        public async UniTask<FirebaseUser> SignInAsync(AuthCredential credential, FirebaseAuth auth)
        {
            var emailCredential = AuthCredentialBuilder.ParseEmail(credential);
            return (await auth.SignInWithEmailAndPasswordAsync(emailCredential.Username, emailCredential.Password)).User;
        }

        public async UniTask<FirebaseUser> LinkAsync(AuthCredential credential, FirebaseUser currentUser, FirebaseAuth auth)
        {
            var emailCredential = AuthCredentialBuilder.ParseEmail(credential);
            var firebaseCredential = EmailAuthProvider.GetCredential(emailCredential.Username, emailCredential.Password);
            return (await currentUser.LinkWithCredentialAsync(firebaseCredential)).User;
        }

        public UniTask ReauthenticateAsync(AuthCredential credential, FirebaseUser user, FirebaseAuth auth)
        {
            var emailCredential = AuthCredentialBuilder.ParseEmail(credential);
            var firebaseCredential = EmailAuthProvider.GetCredential(emailCredential.Username, emailCredential.Password);
            return user.ReauthenticateAsync(firebaseCredential).AsUniTask();
        }
    }
}