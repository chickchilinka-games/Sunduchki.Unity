using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Interfaces;
using ICVR.Tools.Vault.Utils;
using UnityEngine;

namespace ICVR.Tools.Vault.Entities
{
    internal class Connection
    {
        private static Connection _instance;

        private readonly List<IConnectionProcess> _connectionProcesses;
        private CancellationTokenSource _cancellationTokenSource;

        private IConnectionProcess _currentConnectionProcess;
        private VaultConnectionSettings _vaultConnectionSettings;

        private Connection()
        {
            _connectionProcesses = FindConnectionProcesses();
        }

        public static Connection Instance => _instance ??= new Connection();

        public VaultStatus Status { get; private set; } = VaultStatus.Disconnected;
        public string Error { get; private set; }

        public bool Initialize(string credentials, VaultConnectionSettings vaultConnectionSettings)
        {
            if (Status.Equals(VaultStatus.Connecting)) return false;

            _vaultConnectionSettings = vaultConnectionSettings;
            _currentConnectionProcess?.Disconnect();
            _cancellationTokenSource = new CancellationTokenSource();

            Status = VaultStatus.Connecting;

            var authCredentials = SerializationUtils.Deserialize<AuthCredentials>(credentials);

            if (authCredentials == null)
            {
                Debug.LogError("[Vault] Credentials are not correct!");

                return false;
            }

            var authType = authCredentials.AuthType;
            var connectionProcess = _connectionProcesses?.FirstOrDefault(item => item.AuthType.Equals(authType));

            if (connectionProcess == null)
            {
                Debug.LogError($"[Vault] Connection has no auth process with type {authType}!");

                Status = VaultStatus.Failed;

                return false;
            }

            var isSuccess = connectionProcess.Connect(credentials, _cancellationTokenSource.Token);

            if (!isSuccess)
            {
                Debug.LogError("[Vault] Connection was failed!");

                Status = VaultStatus.Failed;

                return false;
            }

            _currentConnectionProcess = connectionProcess;

            Status = VaultStatus.Connected;

            Debug.Log("[Vault] Connection success!");

            return true;
        }

        public string GetRawData()
        {
            return GetRawDataInternal();
        }

        public TData GetData<TData>() where TData : class
        {
            var serializedData = GetRawDataInternal();

            if (string.IsNullOrEmpty(serializedData))
            {
                Debug.LogError("[Vault] Vault data is empty or null!");

                return null;
            }

            return SerializationUtils.Deserialize<TData>(serializedData);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            Status = VaultStatus.Disconnected;
            Error = string.Empty;
        }

        private string GetRawDataInternal()
        {
            if (_currentConnectionProcess == null)
            {
                Debug.LogError("[Vault] The Connection must be initialized!");

                return null;
            }

            var serializedData = _currentConnectionProcess.GetRawData(_vaultConnectionSettings,
                _cancellationTokenSource.Token);

            return serializedData;
        }

        private List<IConnectionProcess> FindConnectionProcesses()
        {
            var processes = new List<IConnectionProcess>();

            var implementations =
                ReflectionUtils.GetImplementationsFromCurrentAssembly<IConnectionProcess>();

            if (implementations == null || !implementations.Any())
            {
                Debug.LogError("[Vault] Connection has no auth processes!");

                return null;
            }

            foreach (var type in implementations)
            {
                var authProcess = ReflectionUtils.CreateInstance<IConnectionProcess>(type);

                processes.Add(authProcess);
            }

            return processes;
        }
    }
}