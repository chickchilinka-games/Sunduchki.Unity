namespace ICVR.Tools.Vault.Data
{
    public class AuthCredentials
    {
        public AuthCredentials(AuthType authType)
        {
            AuthType = authType;
        }

        public AuthType AuthType { get; }
    }
}