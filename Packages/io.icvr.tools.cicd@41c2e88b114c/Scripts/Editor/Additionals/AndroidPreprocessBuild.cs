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

#if UNITY_ANDROID

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ICVR.Tools.Additionals
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