using R3;

namespace Features.AppLifecycle.Services
{
    public class GameMenuFlowService
    {
        private readonly Subject<Unit> _exitRequested = new();

        public Observable<Unit> ExitRequested => _exitRequested;

        public void RequestExit()
        {
            _exitRequested.OnNext(Unit.Default);
        }
    }
}
