using System;
using Core.Model;
using R3;

namespace Core.Services
{
    internal class InternalDebuggerService
    {
        public bool IsDebuggerVisible => _debuggerModel.IsVisible.CurrentValue;
        public Observable<bool> VisibilityChanged => _debuggerModel.IsVisible;

        private readonly ICVRDebuggerModel _debuggerModel;

        public InternalDebuggerService(ICVRDebuggerModel debuggerModel)
        {
            _debuggerModel = debuggerModel;
        }
        public void ShowDebugger()
        {
            if (_debuggerModel.IsInitialized) 
                _debuggerModel.SetVisible(true);
        }
        
        public void HideDebugger()
        {
            if (_debuggerModel.IsInitialized) 
                _debuggerModel.SetVisible(false);
        }
    }
}