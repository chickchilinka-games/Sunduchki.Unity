using System;
using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Interfaces;
using R3;
using UnityEngine;

namespace DebuggerPlugins.Logger.LogSenders
{
    internal class UnityLogSender : ILogSender
    {
        public Observable<LoggerMessage> MessageSent { get; } = UnityLogCallbackAsObservable();

        private static Observable<LoggerMessage> UnityLogCallbackAsObservable()
        {
            return Observable.FromEvent<Application.LogCallback, LoggerMessage>(Conversion,
                callback => Application.logMessageReceived += callback,
                callback => Application.logMessageReceived -= callback);
        }

        private static Application.LogCallback Conversion(Action<LoggerMessage> callback)
        {
            return (message, stackTrace, type) =>
                callback(new LoggerMessage("Unity", type, message, stackTrace));
        }
    }
}