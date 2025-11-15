using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DebuggerPlugins.Logger.Data
{
    public readonly struct LoggerMessage : IEquatable<LoggerMessage>
    {
        public readonly DateTime DateTime;
        public readonly string Category;
        public readonly LogType LogType;
        public readonly string Message;
        public readonly string StackTrace;
        
        private readonly string _id;

        public LoggerMessage(string category, LogType logType, string message, string stackTrace)
        {
            DateTime = DateTime.Now;
            Category = category;
            LogType = logType;
            Message = message;
            StackTrace = stackTrace;

            _id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToStringExtended()
        {
            return ToString(true);
        }

        public bool Equals(LoggerMessage other)
        {
            return _id.Equals(other._id);
        }

        public override bool Equals(object obj)
        {
            return obj is LoggerMessage other && Equals(other);
        }

        public static bool operator ==(LoggerMessage first, LoggerMessage second)
        {
            return first.Category == second.Category &&
                   first.LogType == second.LogType &&
                   first.Message == second.Message &&
                   first.StackTrace == second.StackTrace;
        }

        public static bool operator !=(LoggerMessage first, LoggerMessage second)
        {
            return !(first == second);
        }

        private string ToString(bool stackTrace)
        {
            var result = new StringBuilder();
            result.Append($"[{DateTime.ToLongTimeString()}]");
            result.Append($"[{Category}]");
            result.Append($"[{LogType}]");
            result.Append($" {Message}");

            if (stackTrace)
            {
                result.Append($"\n{StackTrace}");
            }
            
            return result.ToString();
        }
        
        public class Comparer : IEqualityComparer<LoggerMessage>
        {
            public bool Equals(LoggerMessage x, LoggerMessage y)
            {
                return x == y;
            }

            public int GetHashCode(LoggerMessage obj)
            {
                return HashCode.Combine(obj.Category, (int)obj.LogType, obj.Message, obj.StackTrace);
            }
        }
    }
}