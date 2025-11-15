using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Storages;

namespace DebuggerPlugins.Logger.Services
{
    internal class LoggerMessageService
    {
        private readonly LoggerMessagesStorage _loggerMessagesStorage;
        private readonly LoggerCategoriesStorage _loggerCategoriesStorage;

        public LoggerMessageService(LoggerMessagesStorage loggerMessagesStorage, LoggerCategoriesStorage loggerCategoriesStorage)
        {
            _loggerMessagesStorage = loggerMessagesStorage;
            _loggerCategoriesStorage = loggerCategoriesStorage;
        }

        public void AddMessage(LoggerMessage data)
        {
            _loggerMessagesStorage.Add(data);
            _loggerCategoriesStorage.Add(data.Category);
        }
    }
}