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
using ICVR.Window.Models;
using UnityEngine;
using Utils;
using Zenject;

namespace ICVR.Window.Spawners
{
    internal class WindowFactory : IFactory<IContent, ITemplate, IWindow>
    {
        private readonly WindowSystemModel _windowSystemModel;

        public WindowFactory(WindowSystemModel windowSystemModel)
        {
            _windowSystemModel = windowSystemModel;
        }
        
        public IWindow Create(IContent content, ITemplate template)
        {
            var windowGameObject = new GameObject($"{content.Id}({template.Id})");
            var templateRect = template.FrameRect.rect;
            var windowTransform = windowGameObject.TryGetComponent(out RectTransform rectTransform)
                ? rectTransform
                : windowGameObject.AddComponent<RectTransform>();
            windowTransform.ApplyRect(templateRect);
            
            var window = windowGameObject.AddComponent<Basics.Window>();
            window.SetTemplate(template);
            window.SetContent(content);
            window.SetParent(_windowSystemModel.CurrentWindowArea);

            template.SetContent(content);
            template.SetParent(windowTransform);
            
            return window;
        }
    }
}