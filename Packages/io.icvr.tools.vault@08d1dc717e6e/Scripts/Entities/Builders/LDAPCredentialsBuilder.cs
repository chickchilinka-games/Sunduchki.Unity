using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Utils;

namespace ICVR.Tools.Vault.Entities
{
    public class LDAPCredentialsBuilder
    {
        private readonly LDAPCredentials _credentials;

        public LDAPCredentialsBuilder(string nickname, string password)
        {
            _credentials = new LDAPCredentials(AuthType.LDAP, nickname, password);
        }

        public string Build()
        {
            if (_credentials == null
                || string.IsNullOrEmpty(_credentials.Nickname)
                || string.IsNullOrEmpty(_credentials.Password))
                return null;

            return SerializationUtils.Serialize(_credentials);
        }
    }
}