// ICVR CONFIDENTIAL
// __________________
// 
// [2016] -  [2024] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to ICVR LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Basics;
using UnityEngine;

namespace ICVR.Window.Utility
{
    public static class WindowUtility
    {
        public static AbstractContent CreateBrokenContent(string objectName = "broken_content")
        {
            return new GameObject(objectName).AddComponent<BrokenContent>();
        }
        public static AbstractTemplate CreateBrokenTemplate(string objectName = "broken_template")
        {
            return new GameObject(objectName).AddComponent<BrokenTemplate>();
        }
        
    }
}