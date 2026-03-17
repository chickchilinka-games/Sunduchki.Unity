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

using Chickchilinka.Window.Abstract;
using Chickchilinka.Window.Utility;
using UnityEditor;

[CustomPropertyDrawer(typeof(ContentNameAttribute))]
public class ContentNameProperty : BasePropertyDrawer<AbstractContent>
{
}

[CustomPropertyDrawer(typeof(TemplateNameAttribute))]
public class TemplateNameProperty : BasePropertyDrawer<AbstractTemplate>
{
}