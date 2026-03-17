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
using System.Linq;
using Chickchilinka.Window.Utility;
using Chickchilinka.WindowSystemEditor.Utility;
using UnityEditor;
using UnityEngine;


public abstract class BasePropertyDrawer<T> : PropertyDrawer 
{
    private string[] _displayNames;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    { 
        _displayNames ??= BuildTypeList();

        var selectedIndex = Array.FindIndex(_displayNames, data => data.Equals(property.stringValue));
            
        if (selectedIndex == -1)
            selectedIndex = 0;

        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, _displayNames);
        property.stringValue = _displayNames[selectedIndex];
    } 
    
        
    private static string[] BuildTypeList()
    {
        return ReflectionUtility<T>.GetAllTypes()
            .Select(type => NameAttributeUtility.ToDisplayName(type.FullName))
            .Where(displayName => !string.IsNullOrEmpty(displayName)).ToArray();
    }
}