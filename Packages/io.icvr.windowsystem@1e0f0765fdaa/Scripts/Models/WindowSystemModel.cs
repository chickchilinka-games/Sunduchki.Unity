

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