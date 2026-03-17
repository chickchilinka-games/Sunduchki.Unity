/*
    Chickchilinka CONFIDENTIAL
    __________________

    [2016] - [2022] Chickchilinka LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of Chickchilinka LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to Chickchilinka LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from Chickchilinka LLC.
*/


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace Chickchilinka.Tools
{
    public static class VersionMenu
    {
        [MenuItem("Custom/Version/Get Version Number", false, 0)]
        private static void GetVersionNumber()
        {
            EditorUtility.DisplayDialog("Version", AppVersion.FormatToString(), "OK");
        }

        [MenuItem("Custom/Version/Set Version from PlayerSettings", false, 0)]
        private static void SetVersionFromPlayerSettings()
        {
            VersionUtil.LoadVersionFromPlayerSettings();
        }

        [MenuItem("Custom/Version/Apply Current Version", false, 0)]
        private static void ApplyCurrentVersion()
        {
            VersionUtil.ApplyVersion(EditorUserBuildSettings.activeBuildTarget, AppVersion.Current);
        }

        [MenuItem("Custom/Version/Bump Version Number")]
        private static void BumpVersionNumber()
        {
            VersionUtil.IncreaseVersion();
        }

        [MenuItem("Custom/Version/Bump Build Number")]
        private static void BumpBuildNumber()
        {
            VersionUtil.IncreaseBuild();
        }

        [MenuItem("Custom/Version/Sync With Remote", false, 0)]
        private static void SyncWithRemote()
        {
            var currentVersion = AppVersion.Current;
            var remoteVersion = GetFromGit();

            if (remoteVersion > currentVersion)
            {
                var message =
                    $"Current Version: {currentVersion.FormatToString()}\n" +
                    $"Remote Version: {remoteVersion.FormatToString()}\n" +
                    "Apply Remote version locally?";

                var shouldApply = EditorUtility.DisplayDialog("Version", message, "Apply", "Cancel");
                if (shouldApply)
                {
                    VersionUtil.SetVersion(remoteVersion);
                    EditorUtility.DisplayDialog("Version", $"Set Local version to {remoteVersion.FormatToString()}",
                        "OK");
                }
            }
            else if (remoteVersion < currentVersion)
            {
                var message =
                    $"Current Version: {currentVersion.FormatToString()}\n" +
                    $"Remote Version: {remoteVersion.FormatToString()}\n" +
                    "Apply Local version to remote?";

                var shouldApply = EditorUtility.DisplayDialog("Version", message, "Apply", "Cancel");
                if (shouldApply)
                {
                    if (ApplyVersionToGit(currentVersion))
                        EditorUtility.DisplayDialog("Version",
                            $"Set Remote version to {currentVersion.FormatToString()}", "OK");
                    else
                        EditorUtility.DisplayDialog("Version",
                            "There was an exception setting Remote version, see log for errors", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Version",
                    $"Remote and Local version match: {currentVersion.FormatToString()}", "OK");
            }
        }

        private static bool ApplyVersionToGit(Version version)
        {
            // Start the child process.
            var p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
#if UNITY_EDITOR_WIN
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = $"/C git tag {version.FormatToString()} && git push --tags";
#elif UNITY_EDITOR_OSX
            p.StartInfo.FileName = "/bin/bash";
            p.StartInfo.Arguments = $"-c \"git tag {version.FormatToString()} && git push --tags\"";
#else
            throw new NotSupportedException("Platform not supported");
#endif
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException($"Error executing git\n{output}");
            return true;
        }

        private static Version GetFromGit()
        {
            // Start the child process.
            var p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
#if UNITY_EDITOR_WIN
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C git fetch && git ls-remote --tags --quiet --sort=\"-version:refname\"";
#elif UNITY_EDITOR_OSX
            p.StartInfo.FileName = "/bin/bash";
            p.StartInfo.Arguments = "-c \"git fetch && git ls-remote --tags --quiet --sort='-version:refname'\"";
#else
            throw new NotSupportedException("Platform not supported");
#endif
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException($"Error executing git\n{output}");
            return AppVersion.ParseFromString(output, false);
        }
    }
}