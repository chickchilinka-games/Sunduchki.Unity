/*
 ICVR CONFIDENTIAL
 __________________
 
 [2016] - [2023] ICVR LLC
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ICVR.Tools.Additionals.Android
{
    public class AndroidCustomGradleBuildPreProcess : IPreprocessBuildWithReport
    {
        private const string CustomGradleRelativePath = "CUSTOM_GRADLE_PATH";
        private const string CustomGradleVersionVariableName = "CUSTOM_GRADLE_VERSION";
        private const string ProjectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
        private const string UseCustomGradleTemplatePropertyName = "useCustomBaseGradleTemplate";
        private const string ProjectAndroidPluginFolder = "Plugins/Android";
        private const string BaseGradleTemplateFileName = "baseProjectTemplate.gradle";
        private const string GradleToolVersionPattern = @"(com.android.tools.build:gradle:)([\d]+.[\d]+.[\d]+)";
        
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
#if UNITY_ANDROID
            ParseCustomGradleVersion();
#endif
        }

#if UNITY_ANDROID
        private void ParseCustomGradleVersion()
        {
            var gradlePath = Environment.GetEnvironmentVariable(CustomGradleRelativePath);
            var gradleToolVersion = Environment.GetEnvironmentVariable(CustomGradleVersionVariableName);

            if (string.IsNullOrEmpty(gradlePath) || string.IsNullOrEmpty(gradleToolVersion))
            {
                return;
            }
            
            #region Set gradle path
            
            var fullGradlePath = Path.Combine(Application.dataPath.Replace("Assets", gradlePath));
            AndroidExternalToolsSettings.gradlePath = fullGradlePath;

            #endregion

            #region Set gradle version
            
            var projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsAssetPath)[0]);
            var useCustomBaseGradleTemplate = projectSettings.FindProperty(UseCustomGradleTemplatePropertyName);

            var customBaseGradleTemplateFullPath = Path.Combine(Application.dataPath, ProjectAndroidPluginFolder, BaseGradleTemplateFileName);

            if (!useCustomBaseGradleTemplate.boolValue)
            {
                CreateBaseGradleTemplateFile(customBaseGradleTemplateFullPath);
            }
            else
            {
                ReplaceGradleToolVersion(customBaseGradleTemplateFullPath, gradleToolVersion);   
            }

            #endregion
        }

        private void CreateBaseGradleTemplateFile(string customBaseGradleTemplateFullPath)
        {
            if (!Directory.Exists(customBaseGradleTemplateFullPath))
            {
                Directory.CreateDirectory(customBaseGradleTemplateFullPath);
            }
            
            var streamWriter = File.CreateText(customBaseGradleTemplateFullPath);
            var customBaseGradleTemplate = new CustomBaseGradleTemplate();
            streamWriter.Write(customBaseGradleTemplate.Content);
        }
        
        private void ReplaceGradleToolVersion(string customBaseGradleTemplateFullPath, string gradleToolVersion)
        {
            using var readStream = new FileStream(customBaseGradleTemplateFullPath, FileMode.Open, FileAccess.Read);
            
            var bufferLength = (int)readStream.Length;
            var buffer = new byte[bufferLength];

            readStream.Read(buffer, 0, bufferLength);

            var text = Encoding.UTF8.GetString(buffer);
            var regex = new Regex(GradleToolVersionPattern);

            var match = regex.Match(text);

            if (!match.Success || match.Groups.Count < 3)
            {
                ThrowException("Your baseGradleTemplate file in not correct!");
                    
                return;
            }

            var dependencyDeclarationFromFile = match.Groups[1].ToString();
            var versionFromFile = match.Groups[2].ToString();

            readStream.Dispose();
            
            if (versionFromFile.Equals(gradleToolVersion))
            {
                return;
            }
                
            using var writeStream = new FileStream(customBaseGradleTemplateFullPath, FileMode.Create, FileAccess.Write);
            
            text = text.Replace(dependencyDeclarationFromFile + versionFromFile,
                dependencyDeclarationFromFile + gradleToolVersion);

            var bytes = Encoding.UTF8.GetBytes(text);
            
            writeStream.Write(bytes, 0, bytes.Length);
            
            writeStream.Dispose();
        }
        
        private void ThrowException(string reason)
        {
            throw new UnityException($"Custom gradle version wasn't set up! Reason: {reason}");
        }
#endif
    }
}