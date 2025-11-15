// ICVR CONFIDENTIAL
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

using System.Collections.Generic;
using UnityEngine;

namespace ICVR.Window.Data
{
    public class WindowSystemConfig : ScriptableObject
    {
        [SerializeField]
        private bool addressablesSupport;
        
        [SerializeField]
        private string resourcePrefabsRootPath;
        
        [SerializeField]
        private ContentMappingData[] contentMap;
        
        [SerializeField]
        private TemplateMappingData[] templateMap;

        public bool AddressablesSupport => addressablesSupport;
        public string ResourcePrefabsRootPath => resourcePrefabsRootPath;
        public IReadOnlyList<IMappingData> ContentMap => contentMap;
        public IReadOnlyList<IMappingData> TemplateMap => templateMap;
    }
}