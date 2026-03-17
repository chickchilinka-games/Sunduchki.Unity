using System.Threading;
using Chickchilinka.Tools.Vault.Data;

namespace Chickchilinka.Tools.Vault.Interfaces
{
    internal interface IConnectionProcess
    {
        AuthType AuthType { get; }
        bool Connect(string credentials, CancellationToken cancellationToken);
        void Disconnect();

        string GetRawData(VaultConnectionSettings connectionSettings,
            CancellationToken cancellationToken);
    }
}