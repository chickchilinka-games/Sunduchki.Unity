using System.Collections.Generic;
using System.Linq;
using Modules.AuthenticationSystem.Data;
using R3;

namespace Modules.AuthenticationSystem.Entities
{
    public class AuthenticationModel
    {
        public ReadOnlyReactiveProperty<AuthStatus> AuthStatusStream => _authStatusStream;
        public ReadOnlyReactiveProperty<UserContext> UserContextStream => _userContextStream;

        private readonly ReactiveProperty<AuthStatus> _authStatusStream = new(AuthStatus.None);
        private readonly ReactiveProperty<UserContext> _userContextStream = new(null);

        public void StartSignIn()
        {
            _authStatusStream.Value = AuthStatus.SigningIn;
        }

        public void CompleteSignIn(UserContext userContext)
        {
            _userContextStream.Value = userContext;
            _authStatusStream.Value = AuthStatus.SignedIn;
        }

        public void FailSignIn()
        {
            _authStatusStream.Value = AuthStatus.SignedOut;
        }

        public void SignOut()
        {
            _userContextStream.Value = null;
            _authStatusStream.Value = AuthStatus.SignedOut;
        }

        public void Link(LinkageInfo linkageInfo)
        {
            if (_userContextStream.Value == null)
            {
                return;
            }

            var updatedLinked = new List<LinkageInfo>(_userContextStream.Value.LinkedProviders
                .Where(link => link.AuthType != linkageInfo.AuthType))
            {
                linkageInfo
            };

            _userContextStream.Value = new UserContext(
                _userContextStream.Value.UserId,
                _userContextStream.Value.Username,
                _userContextStream.Value.Role,
                updatedLinked);
        }

        public void Unlink(LinkageInfo linkageInfo)
        {
            if (_userContextStream.Value == null)
            {
                return;
            }

            var updatedLinked = new List<LinkageInfo>(_userContextStream.Value.LinkedProviders
                .Where(link => link.AuthType != linkageInfo.AuthType));

            _userContextStream.Value = new UserContext(
                _userContextStream.Value.UserId,
                _userContextStream.Value.Username,
                _userContextStream.Value.Role,
                updatedLinked);
        }
    }
}
