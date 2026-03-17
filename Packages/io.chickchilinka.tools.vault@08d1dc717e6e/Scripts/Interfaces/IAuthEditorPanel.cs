using Chickchilinka.Tools.Vault.Data;
using UnityEngine;

namespace Chickchilinka.Tools.Vault.Interfaces
{
    internal interface IAuthEditorPanel
    {
        AuthType AuthType { get; }
        void Initialize();
        void Draw(Vector2 areaSize);
        string GetCredentials();
    }
}