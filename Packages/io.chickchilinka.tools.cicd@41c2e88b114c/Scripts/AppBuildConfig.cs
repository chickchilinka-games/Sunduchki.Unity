/*
 * This script is modified automatically
 * Do not edit
 */

using UnityEngine;

namespace Chickchilinka.Tools
{
    [CreateAssetMenu(menuName = "CI CD Tools/Create AppBuildConfig", fileName = nameof(AppBuildConfig))]
    public class AppBuildConfig : ScriptableObject
    {
        private static AppBuildConfig _instance;

        // Editor initialization
        public static void Initialize(AppBuildConfig config)
        {
            _instance = config;
            Debug.Log($"Loaded app build config with version {Instance.Version}: InstanceID {Instance.GetInstanceID()}");
        }

        public static AppBuildConfig Instance
        {
            get
            {
                // Runtime lazy initialization
                _instance = _instance ? _instance : Resources.Load<AppBuildConfig>("AppBuildConfig");
                return _instance;
            }
        }

        [field: SerializeField] public string Version { get; private set; } = "1.0b1";
        [field: SerializeField] public string EnvironmentId { get; private set; } = "DEV";

        public void SetVersion(string version)
        {
            Version = version;
        }

        public void SetEnvironment(string environmentId)
        {
            EnvironmentId = environmentId;
        }
    }
}