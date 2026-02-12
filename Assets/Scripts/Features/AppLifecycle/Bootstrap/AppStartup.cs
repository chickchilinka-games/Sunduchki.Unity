using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using UnityEngine;
using Zenject;

namespace Features.AppLifecycle.Bootstrap
{
    public class AppStartup : MonoBehaviour
    {
        private AppLifecycleService _appLifecycleService;
        private CancellationTokenSource _cancellationTokenSource;
        private UniTask _appTask;

        [Inject]
        public void Construct(AppLifecycleService appLifecycleService)
        {
            _appLifecycleService = appLifecycleService;
        }

        private void Awake()
        {
            // Set target frame rate to 60 FPS for consistent performance
            Application.targetFrameRate = 60;
            Debug.Log($"[AppStartup] Target FPS set to: {Application.targetFrameRate}");

            _cancellationTokenSource = new CancellationTokenSource();
            _appTask = _appLifecycleService.StartApp(_cancellationTokenSource.Token);
            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();

            if (_appTask.Status == UniTaskStatus.Pending)
            {
                _appTask.SuppressCancellationThrow().Forget();
            }

            _cancellationTokenSource?.Dispose();
        }
    }
}
