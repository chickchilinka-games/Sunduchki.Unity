using System.Linq;
using Core.Interfaces;
using DebuggerPlugins.Logger.Storages;

namespace DebuggerPlugins.Logger.Services
{
    public class LoggerService : ILogger
    {
        private readonly LoggerMessagesStorage _loggerMessagesStorage;

        internal LoggerService(LoggerMessagesStorage loggerMessagesStorage)
        {
            _loggerMessagesStorage = loggerMessagesStorage;
        }

        public string GetLog(string[] categoryFilter = null)
        {
            if (categoryFilter != null)
            {
                return _loggerMessagesStorage.ToFilteredString(categoryFilter);
            }
            
            return _loggerMessagesStorage.ToString();
        }
    }
}