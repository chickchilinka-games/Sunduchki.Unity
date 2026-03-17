

using System.Collections.Generic;
using UnityEngine;

namespace Chickchilinka.Window.Data
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