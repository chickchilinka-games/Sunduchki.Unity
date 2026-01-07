using R3;

namespace Features.AppLifecycle.Services
{
    public class LobbyFlowService
    {
        private readonly Subject<Unit> _enterRequested = new();
        private readonly Subject<Unit> _exitRequested = new();

        public Observable<Unit> EnterRequested => _enterRequested;
        public Observable<Unit> ExitRequested => _exitRequested;

        public void RequestEnter()
        {
            _enterRequested.OnNext(Unit.Default);
        }

        public void RequestExit()
        {
            _exitRequested.OnNext(Unit.Default);
        }
    }
}
