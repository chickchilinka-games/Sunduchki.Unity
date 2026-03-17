using System;
using System.IO;
using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Entities;
using ICVR.Tools.Vault.Utils;
using ICVR.Tools.Vault.View;
using UnityEngine;

namespace ICVR.Tools.Vault.Services
{
    public class VaultService
    {
        public void ConnectUI(Action<bool> callback, string environment, string projectName, string clientName)
        {
            Connection.Instance.Dispose();

            var connectionSettings = GetConnectionSettings(environment, projectName, clientName);

            VaultMainEditorWindow.Open(callback, connectionSettings);
        }

        public bool Connect(string credentials, string environment, string projectName, string clientName)
        {
            Connection.Instance.Dispose();

            var connectionSettings = GetConnectionSettings(environment, projectName, clientName);

            Connection.Instance.Initialize(credentials, connectionSettings);

            return Connection.Instance.Status.Equals(VaultStatus.Connected);
        }

        public void Disconnect()
        {
            Connection.Instance.Dispose();
        }

        public TData GetData<TData>() where TData : class
        {
            return Connection.Instance.GetData<TData>();
        }

        public string GetRawData()
        {
            return Connection.Instance.GetRawData();
        }

        public bool CreateFileFromProperty(string propertyName, string resultFilePath)
        {
            try
            {
                var rawData = Connection.Instance.GetRawData();
                var propertyValue = SerializationUtils.GetFieldValue<string>(rawData, propertyName);

                var fromBase64String = Convert.FromBase64String(propertyValue);
                var resultFileStream = File.Open(resultFilePath, FileMode.Create, FileAccess.Write);

                using (resultFileStream)
                {
                    resultFileStream.Write(fromBase64String, 0, fromBase64String.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Vault] {e.Message}");

                return false;
            }

            return true;
        }

        private VaultConnectionSettings GetConnectionSettings(string environment, string projectName, string clientName)
        {
            var connectionSettings = new VaultConnectionSettings(
                Enum.Parse<VaultEnvironment>(environment),
                projectName,
                clientName);

            return connectionSettings;
        }
    }
}