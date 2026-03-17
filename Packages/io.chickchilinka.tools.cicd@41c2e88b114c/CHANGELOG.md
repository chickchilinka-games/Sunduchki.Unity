3.1.2

- added the credentials importing from the Vault

2.2.7

- No fixes, just testing CHANGELOG file
- Someone has to add changelog for v2.2!

1.0.30

- absorption cicd-config package

1.0.27

- fix the build existing check for ios

1.0.26

- Bug fix: error with custom gradle version
- Release: check build file existing 

1.0.25

- Release: Added Android Full APK option
- Release: Added custom editor sample, so that it blocks future Editor namespace usage
- Bug Fix: non-android compilation issues
- Bug Fix - overriding Editor namespace causes a bunch of errors in custom components
 
1.0.25-preview.9

- Bug Fix: non-android compilation issues

1.0.25-preview.2

- Bug Fix - overriding Editor namespace causes a bunch of error in custom components
- Added custom editor sample, so that it blocks future Editor namespace usage 

1.0.25-preview.1

- Added Android Full APK option
 
1.0.23

- Added the set custom gradle ability

1.0.22

- Added switch sdk version ability from yml file

1.0.21

- Set IP address field in AcceleratorData
- Set minimum unity version for CI Tools from 2021.2 to 2021.3 

1.0.20

- Fix for version 2021.3+
 
1.0.19

- Fixed compilation error for versions below 2021.3

1.0.18

- Added Unity Accelerator auto-config

1.0.17

- Add method to embed packages from Chickchilinka or other scoped registries  
 
1.0.16

- Set CI_SKIP_BUILD to skip the actual build pipeline (only import of the project)
- Fixes in the README file
 
1.0.15

- Fixed build problem if the project didn't contain ICICustomConfig
- Added Remote sync for MacOS

1.0.14-preview.6

- Moved ICICustomEnvironments from editor to runtime

1.0.14-preview.5

- Added ICICustomEnvironments for implementing custom environments

1.0.14

- New system to store AppVersion and AppEnvironment (compiled through custom package)
- Build Report (saved to buildReport.txt)
- Auto Run option (activated in CI menu)

1.0.13

- Fix Environment application during CI build
- Merged Version & Environment in single AppBuildConfig resource
- Removed Legacy CIEditorScript hooks

1.0.12

- Added Runtime environments stored via AppEnvironmentAsset resource
- Use AppEnvironment.Current to get it

1.0.11

- Added Local<->Remote version synchronization
-
1.0.10

- Added remote version sync

1.0.7

- Added Android code signing support through Env

1.0.6

- Fixes for legacy hooks

1.0.3

- Fix requirement for ICICustomConfig
- Added Legacy hooks (with a warning)

1.0.0

- Release to NPM package registry
- Refactoring
- Improved README

0.2.15

- Cleanup

0.2.14

- Fix dependencies

0.2.13

- Added Addressable building menu

0.2.12

- Added export Android project option

0.2.10

- Apply Android NDK path always

0.2.9

- Added Mono buils for Windows & MacOS
- Added EditorPrefs set through ANDROID_NDK_HOME

0.2.4

- Misc refactoring

0.2.3

- Fixed error reporting

0.2.1

- Added more build options and summary json
- Refactor some stuff
- Fixed menu

0.1.8

- Removed Android x86

0.1.4

- Added Linux build

0.1.3

- Fix AppEnvironment not found

0.1.2

- GUID reference for Editor assembly

0.1.1

- Support version setting from CI_VERSION environment variable in ##.##b## format

0.0.32

- Don't crash on 2019.3 android, instead use x64 build

0.0.30

- Added APP_ID for all platforms

0.0.29

- Change order of build: change platfrom, custom config, version, then build

0.0.28

- Added custom environment support
- Unity 2019.3 support
- Android x86 build pipeline removed for Unity 2019.3

0.0.27

- Change build target before build pipeline

0.0.26

- Report error if scene configuration is invalid
- Report error if platform is not supported

0.0.20

- Compile fix

0.0.19

- Namespace update

0.0.18

- AppEnvironment cleanup and refactor

0.0.17

- Move AppEnvironment outside of Editor scope

0.0.16

- Don't change active target explicitly

0.0.15

- Added Google Play build capabilities (AAB)

0.0.11

- Namespace and name change: Editor -> Global, Environment -> AppEnvironment

0.0.9

- Added environment change from command line, use EnvironmentUtil.SelectEnvironmentFromArgs -env:[id]

0.0.8

- Added environemnt utility
