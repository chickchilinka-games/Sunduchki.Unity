

using System;
using ICVR.Window.Interfaces;
using ICVR.Window.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace ICVR.Window.Rules
{
    internal class LookingForHoldersRule : IInitializable, IDisposable
    {
        private readonly WindowSystemModel _windowSystemModel;

        public LookingForHoldersRule(WindowSystemModel windowSystemModel)
        {
            _windowSystemModel = windowSystemModel;
        }
        
        public void Initialize()
        {
            LookingForHoldersOnScene();
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => LookingForHoldersOnScene(scene);
        private void OnSceneUnloaded(Scene scene) => LookingForHoldersOnScene();

        private void LookingForHoldersOnScene(Scene scene = default)
        {
            var roots = scene != default
                                ? scene.GetRootGameObjects()
                                : SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in roots)
            foreach (var holder in RecursiveLookingForHolder(root.transform))
            {
                _windowSystemModel.SetWindowHolder(holder);
                return;
            }

            Debug.Log($"There are no holder in scene {scene.name}");
            _windowSystemModel.ResetWindowHolder();
        }
        
        private IHolder[] RecursiveLookingForHolder(Transform root)
        {
            return root.GetComponentsInChildren<IHolder>(true);
        }
    }
}