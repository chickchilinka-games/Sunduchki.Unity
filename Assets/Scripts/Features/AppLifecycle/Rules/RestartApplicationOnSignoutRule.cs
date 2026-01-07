using System;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Services;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Features.AppLifecycle.Rules
{
    public class RestartApplicationOnSignoutRule : IInitializable, IDisposable
    {
        private readonly AuthenticationService _authenticationService;

        private IDisposable _subscription;

        public RestartApplicationOnSignoutRule(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public void Initialize()
        {
            _subscription = _authenticationService.AuthStatusStream
                .Where(status => status == AuthStatus.SignedOut)
                .Subscribe(_ => RestartApplication());
        }

        private void RestartApplication()
        {
            Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] SignOut detected, performing full application restart");

            try
            {
                DestroyAllDontDestroyOnLoadObjects();
                
                ClearStaticCaches();
                
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();

                Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] All cleanup completed, restarting application");
                
                SceneManager.LoadScene(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(RestartApplicationOnSignoutRule)}] Error during application restart: {e}");
                SceneManager.LoadScene(0);
            }
        }

        private void DestroyAllDontDestroyOnLoadObjects()
        {
            var dontDestroyOnLoadScene = default(Scene);

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "DontDestroyOnLoad")
                {
                    dontDestroyOnLoadScene = scene;
                    break;
                }
            }

            if (dontDestroyOnLoadScene.IsValid())
            {
                var rootObjects = dontDestroyOnLoadScene.GetRootGameObjects();
                Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] Found {rootObjects.Length} DontDestroyOnLoad objects");

                foreach (var obj in rootObjects)
                {
                    if (obj != null && ShouldDestroyObject(obj))
                    {
                        Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] Destroying: {obj.name}");
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                    else if (obj != null)
                    {
                        Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] Preserving Firebase object: {obj.name}");
                    }
                }
            }
            
            var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allGameObjects)
            {
                if (obj != null && obj.scene.name == "DontDestroyOnLoad" && ShouldDestroyObject(obj))
                {
                    Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] Force destroying: {obj.name}");
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        private bool ShouldDestroyObject(GameObject obj)
        {
            var name = obj.name.ToLower();
            
            if (name.Contains("firebase") ||
                name.Contains("google") ||
                name.Contains("authentication") ||
                name.Contains("firestore") ||
                name.Contains("analytics"))
            {
                return false;
            }

            return true;
        }

        private void ClearStaticCaches()
        {
            try
            {
                Resources.UnloadUnusedAssets();

                Debug.Log($"[{nameof(RestartApplicationOnSignoutRule)}] Static caches cleared");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(RestartApplicationOnSignoutRule)}] Error clearing static caches: {e}");
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}