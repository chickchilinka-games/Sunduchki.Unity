

#if UNITY_ANDROID

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Chickchilinka.Tools.Additionals
{
    public class AndroidPreprocessBuild : IPreprocessBuildWithReport
    {
        private const string SdkPackageVersionEnvironmentVariable = "CI_ANDROID_SDK_PACKAGES";
        private const string SdkPlatformVersionPattern = @"\d+(?=\.{1}\d+\.{1}\d+ platforms)";
        
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            var sdkPackageVersion = Environment.GetEnvironmentVariable(SdkPackageVersionEnvironmentVariable);

            if (string.IsNullOrEmpty(sdkPackageVersion))
            {
                return;
            }

            var match = Regex.Match(sdkPackageVersion, SdkPlatformVersionPattern);

            if (!match.Success)
            {
                return;
            }

            var apiLevelNumber = int.Parse(match.Value);

            if (apiLevelNumber <= 0)
            {
                return;
            }

            var sdkVersion = (AndroidSdkVersions) apiLevelNumber;
            
            PlayerSettings.Android.targetSdkVersion = sdkVersion;
        }
    }
}

#endif