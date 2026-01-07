// ICVR CONFIDENTIAL
// 
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

namespace ICVR.Tools.Additionals
{
    public class CustomBaseGradleTemplate
    {
        public readonly string Content = 
            @"allprojects {
                buildscript {
                    repositories {**ARTIFACTORYREPOSITORY**
                        google()
                        jcenter()
                    }

                    dependencies {
                        classpath 'com.android.tools.build:gradle:4.2.0'
                        **BUILD_SCRIPT_DEPS**
                    }
                }

                repositories {**ARTIFACTORYREPOSITORY**
                    google()
                    jcenter()
                    flatDir {
                        dirs ""${project(':unityLibrary').projectDir}/libs""
                    }
                }
            }

            task clean(type: Delete) {
                delete rootProject.buildDir
            }";
    }
}