using System.Collections.Generic;
using Core.Interfaces;
using DebuggerPlugins.Logger.Interfaces;
using DebuggerPlugins.Logger.Services;
using R3;

namespace DebuggerPlugins.Logger.Rules
{
    internal class LoggerPlugin : IPlugin
    {
        public string Title => "Logger";
        
        private readonly LoggerMessageService _messageService;
        private readonly List<ILogSender> _logSenders;
        private readonly CompositeDisposable _disposables;

        public LoggerPlugin(LoggerMessageService messageService, List<ILogSender> logSenders)
        {
            _logSenders = logSenders;
            _messageService = messageService;
            _disposables = new CompositeDisposable();
        }

        public void Initialize()
        {
            foreach (var logSender in _logSenders)
            {
                logSender.MessageSent.Subscribe(_messageService.AddMessage).AddTo(_disposables);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}