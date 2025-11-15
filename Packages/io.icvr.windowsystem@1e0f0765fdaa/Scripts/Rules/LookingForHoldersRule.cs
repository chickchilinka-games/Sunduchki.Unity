// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

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