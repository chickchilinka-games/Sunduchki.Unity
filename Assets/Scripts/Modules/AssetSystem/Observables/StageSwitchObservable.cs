using System;
using Modules.AppData.Interfaces;
using R3;

namespace Modules.AssetSystem.Observables
{
    public class StageSwitchObservable : AssetUnloadObservable, IAppStateListener
    {
        private readonly Subject<Unit> _onSwitch = new();
        
        protected override IDisposable SubscribeCore(Observer<Unit> observer)
        {
            return _onSwitch.Subscribe(_ => observer.OnNext(Unit.Default),
                observer.OnErrorResume,
                observer.OnCompleted
            );
        }

        public void OnNext(Type stateType)
        {
            _onSwitch.OnNext(Unit.Default);
        }
    }
}
