using System;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public sealed class AuthResult: OperationResult
    {
        private AuthResult(bool succeeded, UserContext userContext, string error): base(succeeded, error)
        {
            UserContext = userContext;
        }

        public UserContext UserContext { get; }

        internal static AuthResult Succeeded(UserContext userContext) =>
            new(true, userContext, null);

        internal static AuthResult Failure(string error) =>
            new(false,null, error);
    }
}
