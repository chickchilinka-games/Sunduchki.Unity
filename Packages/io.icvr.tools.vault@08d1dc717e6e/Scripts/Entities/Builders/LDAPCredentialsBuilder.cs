// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Utils;

namespace ICVR.Tools.Vault.Entities
{
    public class LDAPCredentialsBuilder
    {
        private LDAPCredentials _credentials;
        
        public LDAPCredentialsBuilder(string nickname, string password)
        {
            _credentials = new LDAPCredentials(AuthType.LDAP, nickname, password);
        }

        public string Build()
        {
            if (_credentials == null
                || string.IsNullOrEmpty(_credentials.Nickname)
                || string.IsNullOrEmpty(_credentials.Password))
            {
                return null;
            }

            return SerializationUtils.Serialize(_credentials);
        }
    }
}