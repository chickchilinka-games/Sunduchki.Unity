using System;

namespace Modules.AuthenticationSystem.Exceptions
{
    /// <summary>
    /// Exception thrown when user cancels authentication or authorization flow
    /// </summary>
    public class UserCancelledException : Exception
    {
        public UserCancelledException() : base("User cancelled the authorization.")
        {
        }

        public UserCancelledException(string message) : base(message)
        {
        }

        public UserCancelledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
