using R3;
using DebuggerPlugins.Logger.Data;

namespace DebuggerPlugins.Logger.Interfaces
{
    public interface ILogSender
    {
        public Observable<LoggerMessage> MessageSent { get; }
    }
}