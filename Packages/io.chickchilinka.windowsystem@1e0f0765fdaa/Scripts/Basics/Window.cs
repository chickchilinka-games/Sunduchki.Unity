

using System;
using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Interfaces;
using R3;
using UnityEngine;

namespace Chickchilinka.Window.Basics
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