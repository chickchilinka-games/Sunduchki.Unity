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

using ICVR.Window.Interfaces;
using UnityEngine;

namespace ICVR.Window.Models
{
    internal class WindowSystemModel
    {
        private IHolder _currentWindowHolder;

        public RectTransform CurrentWindowArea => _currentWindowHolder?.Area;

        public void SetWindowHolder(IHolder holder)
        {
            if (holder == null || holder.Area == null)
            {
                Debug.LogError("You are trying to set incorrect holder. Make sure that one is added onto the cancas or it's children.");
                
                return;
            }
            
            _currentWindowHolder = holder;
        }

        public void ResetWindowHolder()
        {
            _currentWindowHolder = null;
        }
    }
}