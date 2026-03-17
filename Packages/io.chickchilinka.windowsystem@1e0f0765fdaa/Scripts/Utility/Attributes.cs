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

using UnityEngine;

namespace Chickchilinka.Window.Utility
{
    public class ContentNameAttribute : PropertyAttribute
    { }
    
    public class TemplateNameAttribute : PropertyAttribute
    { }

    public static class NameAttributeUtility
    {
        public static string ToTypeFullName(string displayName)
        {
            return displayName?.Replace('/', '.');
        }
        public static string ToDisplayName(string displayName)
        {
            return displayName?.Replace('.', '/');
        }
    }
}