#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;

namespace Modules.SignalR
{
    internal interface IWebGLSignalRManagedConnection
    {
        void HandleMessage(SignalRMessageEnvelope envelope);
    }

    internal sealed class SignalRMessageEnvelope
    {
        [JsonProperty("connectionId")] public int ConnectionId { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("handler")] public string Handler { get; set; }
        [JsonProperty("args")] public string ArgsJson { get; set; }
        [JsonProperty("requestId")] public string RequestId { get; set; }
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
        [JsonProperty("result")] public string ResultJson { get; set; }
    }

    internal sealed class WebGLSignalRBridgeHost : MonoBehaviour
    {
        private static WebGLSignalRBridgeHost _instance;
        public static WebGLSignalRBridgeHost Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                var go = new GameObject("SignalRBridgeHost");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<WebGLSignalRBridgeHost>();
                return _instance;
            }
        }

        private readonly Dictionary<int, IWebGLSignalRManagedConnection> _connections = new();

        public int CreateConnection(string url, string accessToken)
        {
            var id = Sunduchki_SignalR_CreateConnection(url, accessToken ?? string.Empty, gameObject.name);
            if (id >= 0)
            {
                _connections[id] = null;
            }

            return id;
        }

        public void Attach(int connectionId, IWebGLSignalRManagedConnection managedConnection)
        {
            if (!_connections.ContainsKey(connectionId))
            {
                _connections.Add(connectionId, managedConnection);
                return;
            }

            _connections[connectionId] = managedConnection;
        }

        public void RemoveConnection(int connectionId)
        {
            _connections.Remove(connectionId);
        }

        public void StartConnection(int connectionId)
        {
            Sunduchki_SignalR_Start(connectionId);
        }

        public void StopConnection(int connectionId)
        {
            Sunduchki_SignalR_Stop(connectionId);
        }

        public void RegisterHandler(int connectionId, string handlerName)
        {
            Sunduchki_SignalR_RegisterHandler(connectionId, handlerName);
        }

        public void UnregisterHandler(int connectionId, string handlerName)
        {
            Sunduchki_SignalR_UnregisterHandler(connectionId, handlerName);
        }

        public void Invoke(int connectionId, string requestId, string methodName, string argsJson)
        {
            Sunduchki_SignalR_Invoke(connectionId, requestId, methodName, argsJson ?? string.Empty);
        }

        public void OnSignalRMessage(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }

            SignalRMessageEnvelope envelope = null;
            try
            {
                envelope = JsonConvert.DeserializeObject<SignalRMessageEnvelope>(payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SignalRBridgeHost] Failed to parse payload: {ex.Message}");
            }

            if (envelope == null)
            {
                return;
            }

            Debug.Log($"[SignalRBridgeHost] message type={envelope.Type}, handler={envelope.Handler}, id={envelope.ConnectionId}");
            if (_connections.TryGetValue(envelope.ConnectionId, out var managed) && managed != null)
            {
                managed.HandleMessage(envelope);
            }
        }

        [DllImport("__Internal")]
        private static extern int Sunduchki_SignalR_CreateConnection(string url, string accessToken, string hostObjectName);

        [DllImport("__Internal")]
        private static extern void Sunduchki_SignalR_Start(int connectionId);

        [DllImport("__Internal")]
        private static extern void Sunduchki_SignalR_Stop(int connectionId);

        [DllImport("__Internal")]
        private static extern void Sunduchki_SignalR_RegisterHandler(int connectionId, string handlerName);

        [DllImport("__Internal")]
        private static extern void Sunduchki_SignalR_UnregisterHandler(int connectionId, string handlerName);

        [DllImport("__Internal")]
        private static extern void Sunduchki_SignalR_Invoke(int connectionId, string requestId, string methodName, string argsJson);
    }
}
#else
namespace Modules.SignalR
{
    internal sealed class WebGLSignalRBridgeHost
    {
        public static WebGLSignalRBridgeHost Instance => throw new System.PlatformNotSupportedException("SignalR bridge доступен только в WebGL билдах.");
    }
}
#endif
