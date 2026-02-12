using System;
using System.Linq;
using Firebase;
using Firebase.Auth;
using Modules.AuthenticationSystem.Exceptions;

namespace Modules.AuthenticationSystem.Utils
{
    public class FirebaseExceptionUtils
    {
        private static FirebaseException UnwrapFirebaseException(Exception ex)
        {
            if (ex is FirebaseException firebaseException)
                return firebaseException;

            if (ex is AggregateException aggregateException)
                return aggregateException.InnerExceptions.OfType<FirebaseException>().FirstOrDefault();

            return null;
        }
        
        
        public static Exception HandleAuthException(Exception ex)
        {
            var firebaseException = UnwrapFirebaseException(ex);
            if (firebaseException == null)
                return ex;

            var code = (AuthError)firebaseException.ErrorCode;

            switch (code)
            {
                case AuthError.Cancelled:
                case AuthError.WebContextCancelled:
                    return new UserCancelledException();

                default:
                    return ex;
            }
        }
    }
}
