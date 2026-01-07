/*
    ICVR CONFIDENTIAL
    __________________

    [2016] - [2022] ICVR LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of ICVR LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to ICVR LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from ICVR LLC.
*/


using System;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ICVR.Tools
{
    internal static class VersionUtil
    {
        public static void ApplyVersion(BuildTarget buildTarget, Version version)
        {
            PlayerSettings.bundleVersion = $"{version.Major}.{version.Minor}";
            PlayerSettings.iOS.buildNumber = version.Build.ToString();
            PlayerSettings.Android.bundleVersionCode = version.Build;
            PlayerSettings.macOS.buildNumber = version.Build.ToString();

            if (buildTarget == BuildTarget.StandaloneOSX || buildTarget == BuildTarget.StandaloneWindows64)
                PlayerSettings.bundleVersion = $"{version.Major}.{version.Minor}b{version.Build}";

            Debug.Log($"[VersionUtil] Apply version: {version.FormatToString()}");
        }

        public static Version LoadVersionFromPlayerSettings()
        {
            var major = AppVersion.Default.Major;
            var minor = AppVersion.Default.Minor;
            var build = AppVersion.Default.Build;

            var versions = PlayerSettings.bundleVersion.Split('.');

            if (versions.Length >= 1) int.TryParse(versions[0], out major);
            if (versions.Length >= 2) int.TryParse(versions[1], out minor);

            if (Application.platform == RuntimePlatform.IPhonePlayer
                || Application.platform == RuntimePlatform.WindowsEditor)
                int.TryParse(PlayerSettings.iOS.buildNumber, out build);
            else if (Application.platform == RuntimePlatform.Android) build = PlayerSettings.Android.bundleVersionCode;
            return new Version(major, minor, build);
        }

        public static void IncreaseVersion()
        {
            var version = AppVersion.Current;
            var newVersion = new Version(version.Major, version.Minor + 1, version.Build);
            CIBootstrap.SaveVersion(newVersion);
        }

        public static void IncreaseBuild()
        {
            var version = AppVersion.Current;
            var newVersion = new Version(version.Major, version.Minor, version.Build + 1);
            CIBootstrap.SaveVersion(newVersion);
        }

        public static Version DetectCurrentVersion()
        {
#if CICD_CONFIG
            return AppVersion.Current;
#else
            return LoadVersionFromPlayerSettings();
#endif
        }

        public static void SetVersionFromString(string versionText)
        {
            SetVersion(AppVersion.ParseFromString(versionText));
        }

        public static void SetVersion(Version version)
        {
            CIBootstrap.SaveVersion(version);
        }
    }
}