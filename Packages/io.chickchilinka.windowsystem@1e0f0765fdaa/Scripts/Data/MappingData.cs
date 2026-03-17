// Chickchilinka CONFIDENTIAL
// __________________
// 
// [2016] -  [2024] Chickchilinka LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of Chickchilinka LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to Chickchilinka LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from Chickchilinka LLC.

using System;
using Chickchilinka.Window.Utility;
using UnityEngine;

namespace Chickchilinka.Window.Data
{
    public interface IMappingData
    {
        string Id { get; }
        public string TypeFullName { get; }
    }
    
    [Serializable]
    public class ContentMappingData : IMappingData
    {
        public string Id => _contentId;
        public string TypeFullName => NameAttributeUtility.ToTypeFullName(_typeName);
        [SerializeField]
        public string _contentId;
        [ContentName, SerializeField]
        private string _typeName;
    }
    
    [Serializable]
    public class TemplateMappingData : IMappingData
    {
        public string Id => _templateId;
        public string TypeFullName => NameAttributeUtility.ToTypeFullName(_typeName);
        [SerializeField]
        public string _templateId;
        [TemplateName, SerializeField]
        private string _typeName;
    }
}