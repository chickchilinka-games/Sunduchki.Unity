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

using System;
using Cysharp.Threading.Tasks;
using ICVR.Window.Interfaces;
using R3;
using UnityEngine;

namespace ICVR.Window.Basics
{
    [RequireComponent(typeof(CanvasGroup))]
    internal class Window : MonoBehaviour, IWindow
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsShown { get; private set; } = false;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (!_canvasGroup)
                    _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }
        
        IContent IWindow.Content => _content;
        ITemplate IWindow.Template => _template;

        private ITemplate _template;
        private IContent _content;
        private CanvasGroup _canvasGroup;
        
        public void SetTemplate(ITemplate template)
        {
            _template = template;
        }

        public void SetContent(IContent content)
        {
            _content = content;
        }
        
        public async UniTask Show()
        {
            Enable();
            await _template.Show();
        }

        public async UniTask Hide()
        {
            await _template.Hide();
            Disable();
        }

        public void SetParent(RectTransform parent)
        {
            transform.SetParent(parent, false);
            
            Disable();
        }
        
        private void Disable()
        {
            IsShown = false;
            CanvasGroup.alpha = 0;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        private void Enable()
        {
            IsShown = true;
            CanvasGroup.alpha = 1;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}