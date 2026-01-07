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

namespace ICVR.Tools.Accelerator
{
    public static class AcceleratorUtils
    {
        public static AcceleratorData GetAcceleratorData()
        {
            var acceleratorData = new AcceleratorData
            {
                CacheServerMode = EditorSettings.cacheServerMode,
                CacheServerEndpoint = EditorSettings.cacheServerEndpoint,
                CacheServerNamespacePrefix = EditorSettings.cacheServerNamespacePrefix,
                CacheServerEnableDownload = EditorSettings.cacheServerEnableDownload,
                CacheServerEnableUpload = EditorSettings.cacheServerEnableUpload,
                CacheServerEnableTls = EditorSettings.cacheServerEnableTls,
#if UNITY_2021_3_OR_NEWER
                CacheServerValidationMode = CacheServerValidationMode.Enabled
#endif
            };

            return acceleratorData;
        }
    }
}