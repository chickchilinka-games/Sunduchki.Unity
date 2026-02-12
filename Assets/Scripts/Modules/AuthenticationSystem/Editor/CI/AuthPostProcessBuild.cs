#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using AppleAuth.Editor;

namespace Modules.AuthenticationSystem.Editor.CI
{
    public class AuthPostProcessBuild
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
//             var projectPath = PBXProject.GetPBXProjectPath(path);
//             var project = new PBXProject();
//             project.ReadFromFile(projectPath);
//
//             var packageName = UnityEngine.Application.identifier;
//             var name = packageName.Substring(packageName.LastIndexOf('.') + 1);
//             var entitlementFileName = name + ".entitlements";
//
// #if UNITY_2019_3_OR_NEWER
//             var projectCapabilityManager = new ProjectCapabilityManager(projectPath, entitlementFileName, null, project.GetUnityMainTargetGuid());
// #else
//             ProjectCapabilityManager projectCapabilityManager = new ProjectCapabilityManager(projectPath, entitlementFileName, PBXProject.GetUnityTargetName());
// #endif
//             projectCapabilityManager.AddSignInWithAppleWithCompatibility();
//             projectCapabilityManager.WriteToFile();
        }
    }
}
#endif
