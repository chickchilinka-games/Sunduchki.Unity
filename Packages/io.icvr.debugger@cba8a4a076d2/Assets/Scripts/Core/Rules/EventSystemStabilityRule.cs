using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Rules
{
    public class EventSystemStabilityRule : IRule, IInitializable, IDisposable
    {
        private EventSystem _eventSystem;

        public void Initialize()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EnsureEventSystemConfigured();
        }

        public void Dispose()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            EnsureEventSystemConfigured();
        }

        private void EnsureEventSystemConfigured()
        {
            _eventSystem = EventSystem.current;

            if (_eventSystem == null)
            {
                Debug.LogError("No EventSystem found in the scene. Ensure an EventSystem is added to your scenes.");
                return;
            }

            var requiredInputModuleType = GetRequiredInputModuleType();
            if (requiredInputModuleType == null)
            {
                Debug.LogError("Unsupported Input Framework. Please check your configuration.");
                return;
            }

            // Check if the current BaseInputModule matches the required type
            var currentInputModule = _eventSystem.GetComponent<BaseInputModule>();
            if (currentInputModule == null || currentInputModule.GetType() != requiredInputModuleType)
            {
                ReplaceInputModule(requiredInputModuleType);
            }
        }

        private static Type GetRequiredInputModuleType()
        {
#if ENABLE_INPUT_SYSTEM
            return typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule);
#endif
            return typeof(StandaloneInputModule);
        }

        private void ReplaceInputModule(Type inputModuleType)
        {
            // Destroy the existing input module if it exists
            var existingInputModule = _eventSystem.GetComponent<BaseInputModule>();
            if (existingInputModule != null)
            {
                Object.Destroy(existingInputModule);
            }

            // Add the required input module
            _eventSystem.gameObject.AddComponent(inputModuleType);
            Debug.Log($"Replaced BaseInputModule with {inputModuleType.Name}");
        }
    }
}
