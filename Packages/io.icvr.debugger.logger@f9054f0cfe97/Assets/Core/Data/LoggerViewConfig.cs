using System;
using DebuggerPlugins.Logger.View;
using UnityEngine;

namespace DebuggerPlugins.Logger.Data
{
    [CreateAssetMenu]
    internal class LoggerViewConfig : ScriptableObject
    {
        public LoggerWindowView loggerWindowViewPrefab;
        public MessageView messageViewPrefab;
        public LogColorInfo[] logColors;

        [Serializable]
        public struct LogColorInfo
        {
            [field: SerializeField] public Color Color { get; private set; }
            [field: SerializeField] public LogType LogType { get; private set; }
        }
    }
}