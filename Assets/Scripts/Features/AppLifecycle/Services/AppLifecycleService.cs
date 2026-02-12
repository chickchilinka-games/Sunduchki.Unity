using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Boot;

namespace Features.AppLifecycle.Services
{
    public class AppLifecycleService
    {
        private readonly IAppStateMachine _appStateMachine;

        public AppLifecycleService(IAppStateMachine appStateMachine)
        {
            _appStateMachine = appStateMachine;
        }

        public UniTask StartApp(CancellationToken token)
        {
            return _appStateMachine.Execute<BootState>(token);
        }
    }
}
