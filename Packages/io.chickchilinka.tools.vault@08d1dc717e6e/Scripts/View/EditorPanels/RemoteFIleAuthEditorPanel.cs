using Chickchilinka.Tools.Vault.Data;
using Chickchilinka.Tools.Vault.Interfaces;
using UnityEngine;

namespace Chickchilinka.Tools.Vault.View
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