namespace ICVR.Tools.Vault.Data
{
    internal class LDAPCredentials : AuthCredentials
    {
        public LDAPCredentials(AuthType authType, string nickname, string password) : base(authType)
        {
            Nickname = nickname;
            Password = password;
        }

        public string Nickname { get; }
        public string Password { get; }
    }
}