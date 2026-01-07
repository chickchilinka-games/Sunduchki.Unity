# CI Tools

## Version 3.0 was published ##

* You should get access to [the Vault](https://vault.icvr.xyz/ui/vault/dashboard) by #helpdesk
    ** In the editor you can authorize by LDAP
* Before build you should make sure your project's key files, ids, tokens and etc are added to [the Vault](https://vault.icvr.xyz/ui/vault/dashboard) for each environments.
* You should receive that values and set they up in the ApplyEnvironment method in the ICICustomConfig implementation in your project. 
* Remove all secret values from your project, include "magic string", configs, keystore files, firebase files and etc.

## Installation

This package will be published to NPM registry.  To use:

1. Make sure you've added ICVR scoped registry to Unity. [Here is how](https://docs.icvr.io/en/General/General/%F0%9F%92%A1+How-to+articles/Working+with+internal+NPM+repository)
2. Add dependency to the `Packages/manifest.json' file:

   ```
   "io.icvr.tools.cicd": "3.0.12"
   ```

   where 3.0.12 is the package version

3. Open Unity and the package should resolve without errors

> Alternatively you can use the Unity Package Manager UI

## Usage

* Used by Internal CI pipeline
* Will add `Custom/CI/` menu with build options
* Will add `Custom/Environment` menu with environment management options
* Will add `AppVersion` static class with version information
* Assembly Definition files are supported

### AppVersion

Version is saved in internal `Resources/AppVersion.asset` file and is loaded via
Unity Resources system upon first call to `AppVersion.Version` property.

### Building steps

1. Build is initiated either from **Menu** or from the **CI Script**
2. *Pre-build* platform-dependant functions are called
	  * For Android the sdk version is set from the CI_ANDROID_SDK_PACKAGES variable (if defined), for example: "build-tools;31.0.0 platforms;android-31" will set android sdk to 31.
	  * For Android the signing options are set from the Environment variables, see [here](https://git.icvr.io/icvr/general/templates/-/blob/master/unity-flow/README.md#android-signing)
3. _(optional)_ `ICICustomConfig.GetSceneListForTarget(BuildTarget)` is called to get the paths of scenes to build
4. `BuildOptions` are prepared:  
   * Default options are set by the selected build type
   * Overrides are loaded from the `CI_BUILD_OPTIONS` environment variable
5. Active build target is changed (may trigger asset reimport)
6. Application version is loaded:
   * Default version is taken from the `AppVersion.asset`
   * Override is loaded from the `CI_VERSION` environment variable
7. Application ID (Bundle ID) is determined:
   * If the `APP_ID` environment variable is set, Application ID will be taken from it and applied
   * Otherwise, it will be taken from the `PlayerSettings` properties
8. _(optional)_ `ICICustomConfig.ApplyCustomSetup()` is called
9. Application version is applied
10. Scene paths and build tools are validated
11. Environment is loaded:
    * Currently selected environment will be used
    * Override is loaded from `CI_ENV` environment variable
12. _(optional)_ Environment is applied by a `ICICustomConfig.ApplyEnvironment(AppEnvironment)` call
13. _(optional)_ If `ICICustomConfig` exists, `ApplyCustomSetup` function is called
14. `BuildOptions.Development` flag is set, if option is selected in the editor (`Custom/CI/Development Build`)
15. `BuildOPtions.AutoRunPlayer` flag is set, if option is selected in the editor (`Custom/CI/Auto Run`) and the build is not run from the CI machine 
16. If `CI_SKIP_BUILD` is set, the rest of the operations are skipped
17. 🤖 Player is built by the UnityEditor BuildPipeline
18. Build result is checked 
    * ⚠️**warning:** sometimes Unity can't determine if the build was success or not, always check the logs and the resulting folder
19. On success, build summary file is prepared and saved to the build folder as `summary.json`
20. On success, build report is generated and saved to the build folder as `buildReport.txt`
21. *Post-build* platform-dependant functions are called
22. _(optional)_ `ICICustomConfig.PostProcessBuild()` is called 

### Adding custom build steps

1. Add a class in Unity Editor script which implements `ICICustomConfig` interface
   * Typical placement `Assets/Plugins/Editor/` 
2. Implement the needed functions
3. Sample file

```c#
using UnityEditor;
using UnityEngine;

namespace Whatever
{
    public class CICustomConfig : ICICustomConfig
    {
        public void ApplyCustomSetup()
        {
            Debug.Log("Simon says: ApplyCustomConfig");
        }

        public string[] GetSceneListForTarget(BuildTarget buildTarget)
        {
            Debug.Log($"Simon says: GetSceneListForTarget({buildTarget})");
            return null;
        }

        public void PostProcessBuild()
        {
            Debug.Log("Simon says: PostProcessBuild");
        }

        public void ApplyEnvironment(AppEnvironment environment)
        {
            Debug.Log($"Simon says: ApplyEnvironment({environment})");
        }
    }
}
```

### Adding custom environment variables

1. Add a class in Unity Editor script which implements `ICICustomEnvironments` interface
   * Typical placement `Assets/Plugins/`
2. Implement the needed functions 
3. Sample file

```c#
using ICVR.Tools;
using UnityEngine;

public class CustomEnvironments : ICICustomEnvironments
{
    public static AppEnvironment MesaEnvironment { get; private set; }

    public void Initialize()
    {
        Debug.Log("[CustomEnvironments] Create custom environments");
        
        MesaEnvironment = AppEnvironment.CreateEnvironment("MESA");
    }
}
```

### Set up castom gradle version

1. Download target gradle binaries from the link: https://gradle.org/releases/
2. Move the downloaded folder to the project folder (e.g. *ProjectName*/gradle-6.7.1) 
3. Make sure you have already added .jar files from the gradle folder into the .gitattributes file
4. Set the `CUSTOM_GRADLE_PATH` variable (relative to the project path, ex. "gradle-6.7.1")
5. Set the `CUSTOM_GRADLE_VERSION` variable (it will be set in the `baseProjectTemplate.gradle` file, e.g "4.2.0")

    Sample (ci.yml):
    ```
    ...
    CUSTOM_GRADLE_PATH: "gradle-6.7.0"
    CUSTOM_GRADLE_VERSION: 4.2.0
    ...
    ```

### Summary file

Sample output:

```json
{
   "buildDate": "2021-08-04T12:21:19.5540573Z",
   "appId": "com.ICVR.io.icvr.tools.cicd",
   "version": "1.1b1",
   "environment": "TEST",
   "buildTarget": "StandaloneWindows64",
   "buildTargetGroup": "Standalone",
   "scriptingBackend": "Mono2x",
   "buildType": "Development",
   "buildOptions": "CompressTextures, StripDebugSymbols, ForceOptimizeScriptCompilation, Il2CPP, Development"
}
```
