/*
    ICVR CONFIDENTIAL
    __________________

    2016 - 2023 ICVR LLC
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



using UnityEditor;
using UnityEngine;

namespace ICVR.Tools.Accelerator
{
    [InitializeOnLoad]
    public static class AcceleratorSetupStep
    {
        static AcceleratorSetupStep()
        {
            return; //Temporary turn off accelerator
            
            var acceleratorData = AcceleratorData.Default;
            
            if (!AcceleratorUtils.GetAcceleratorData().Equals(acceleratorData))
            {
                EditorSettings.cacheServerEndpoint = acceleratorData.CacheServerEndpoint;
                EditorSettings.cacheServerNamespacePrefix = acceleratorData.CacheServerNamespacePrefix;
                EditorSettings.cacheServerEnableDownload = acceleratorData.CacheServerEnableDownload;
                EditorSettings.cacheServerEnableUpload = acceleratorData.CacheServerEnableUpload;
                EditorSettings.cacheServerEnableTls = acceleratorData.CacheServerEnableTls;
#if UNITY_2021_3_OR_NEWER
                EditorSettings.cacheServerValidationMode = acceleratorData.CacheServerValidationMode;
#endif

                EditorSettings.cacheServerMode = acceleratorData.CacheServerMode;
                Debug.Log($"Unity Accelerator initialized ({acceleratorData.CacheServerEndpoint})!"); 
            }
        }
    }
}
