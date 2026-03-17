/*
    Chickchilinka CONFIDENTIAL
    __________________

    2016 - 2023 Chickchilinka LLC
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
using UnityEditor;

namespace Chickchilinka.Tools.Accelerator
{
    public struct AcceleratorData
    {
        public CacheServerMode CacheServerMode;
        public string CacheServerEndpoint;
        public string CacheServerNamespacePrefix;
        public bool CacheServerEnableDownload;
        public bool CacheServerEnableUpload;
        public bool CacheServerEnableTls;
#if UNITY_2021_3_OR_NEWER
        public CacheServerValidationMode CacheServerValidationMode;
#endif

        public static AcceleratorData Default =>
                new AcceleratorData
                {
                    CacheServerMode = CacheServerMode.Enabled,
                    CacheServerEndpoint = "unity-accelerator.chickchilinka.xyz:10443",
                    CacheServerNamespacePrefix = "default",
                    CacheServerEnableDownload = true,
                    CacheServerEnableUpload = true,
                    CacheServerEnableTls = false,
#if UNITY_2021_3_OR_NEWER
                    CacheServerValidationMode = CacheServerValidationMode.Enabled
#endif
                };
        
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            
            var data = (AcceleratorData)obj;
            var isEqual = data.CacheServerMode.Equals(CacheServerMode)
                          && data.CacheServerEndpoint.Equals(CacheServerEndpoint)
                          && data.CacheServerNamespacePrefix.Equals(CacheServerNamespacePrefix)
                          && data.CacheServerEnableDownload.Equals(CacheServerEnableDownload)
                          && data.CacheServerEnableUpload.Equals(CacheServerEnableUpload)
                          && data.CacheServerEnableTls.Equals(CacheServerEnableTls);
            
#if UNITY_2021_3_OR_NEWER
            isEqual = isEqual && data.CacheServerValidationMode.Equals(CacheServerValidationMode);
#endif

            return isEqual;
        }

        public bool Equals(AcceleratorData other)
        { 
            var isEqual = CacheServerMode == other.CacheServerMode 
                   && CacheServerEndpoint == other.CacheServerEndpoint 
                   && CacheServerNamespacePrefix == other.CacheServerNamespacePrefix 
                   && CacheServerEnableDownload == other.CacheServerEnableDownload 
                   && CacheServerEnableUpload == other.CacheServerEnableUpload 
                   && CacheServerEnableTls == other.CacheServerEnableTls;
#if UNITY_2021_3_OR_NEWER
                isEqual = isEqual && CacheServerValidationMode == other.CacheServerValidationMode;
#endif
            return isEqual;
        }

        public override int GetHashCode()
        {
#if UNITY_2021_3_OR_NEWER
            return HashCode.Combine((int)CacheServerMode, CacheServerEndpoint, CacheServerNamespacePrefix, CacheServerEnableDownload, CacheServerEnableUpload, CacheServerEnableTls, (int) CacheServerValidationMode);
#elif UNITY_2021_1_OR_NEWER            
            return HashCode.Combine((int)CacheServerMode, CacheServerEndpoint, CacheServerNamespacePrefix, CacheServerEnableDownload, CacheServerEnableUpload, CacheServerEnableTls);
#else
            return (CacheServerMode, CacheServerEndpoint, CacheServerNamespacePrefix, 
                CacheServerEnableDownload, CacheServerEnableUpload, CacheServerEnableTls).GetHashCode();
#endif
        }
    }
}
