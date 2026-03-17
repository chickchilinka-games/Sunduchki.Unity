using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Interfaces;
using UnityEngine;

namespace ICVR.Tools.Vault.View
{
    internal class RemoteFIleAuthEditorPanel : IAuthEditorPanel
    {
        public AuthType AuthType => AuthType.RemoteFile;

        public void Initialize()
        {
        }

        public void Draw(Vector2 areaSize)
        {
        }

        public string GetCredentials()
        {
            return string.Empty;
        }
    }
}