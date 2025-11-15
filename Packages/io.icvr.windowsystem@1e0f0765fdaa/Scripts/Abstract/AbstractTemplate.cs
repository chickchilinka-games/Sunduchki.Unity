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

namespace ICVR.Window.Abstract
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class AbstractTemplate : MonoBehaviour, ITemplate
    {
        private readonly CompositeDisposable _templateSubscriptions = new();
        
        [SerializeField] 
        private RectTransform _frameRect;
        [SerializeField]
        private RectTransform _contentArea;
        protected string Title { get; set;  }
        void ICloseListener.SubscribeOnClose(Action onClosed) => _closeAction += onClosed;
        void ICloseListener.UnsubscribeOnClose(Action onClosed) => _closeAction -= onClosed;
        void ICloseListener.ClearCloseSubscriptions() => _closeAction = null;
        
        private Action _closeAction;

        public string Id => GetType().Name;
        public RectTransform FrameRect => _frameRect;
        public abstract UniTask Show();
        public abstract UniTask Hide();
        protected void Close() => _closeAction?.Invoke();

        public virtual void SetParent(RectTransform parent)
        {
            transform.SetParent(parent, false);
        }

        protected virtual void OnTitleChanged(string title)
        {
            Title = title;
        }

        protected virtual void OnDisable()
        {
            _templateSubscriptions.Clear();
        }

        void ITemplate.SetContent(IContent content)
        {
            OnTitleChanged(content.Title);
            content.TitleChanged.Subscribe(OnTitleChanged).AddTo(_templateSubscriptions);
            
            content.SetParent(_contentArea);
        }
    }
}