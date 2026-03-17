using System;
using Chickchilinka.Window.Basics;

namespace Features.WindowSystemImpl.Data
{
    public struct ErrorWindowContentData : IWindowData
    {
        public bool Fatal { get; }
        public Action OnClose { get; }
        public string ErrorMessage { get; }
        
        public ErrorWindowContentData(string errorMessage, Action onClose, bool fatal)
        {
            ErrorMessage = errorMessage;
            OnClose = onClose;
            Fatal = fatal;
        }
    }
}
