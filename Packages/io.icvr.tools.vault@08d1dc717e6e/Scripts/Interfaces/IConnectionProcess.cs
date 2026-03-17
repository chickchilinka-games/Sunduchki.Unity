using System.Threading;
using ICVR.Tools.Vault.Data;

namespace ICVR.Tools.Vault.Interfaces
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