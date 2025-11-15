using System;

namespace Modules.AppData.Interfaces
{
    public interface IAppStateListener
    {
        public void OnNext(Type stateType);
    }
}