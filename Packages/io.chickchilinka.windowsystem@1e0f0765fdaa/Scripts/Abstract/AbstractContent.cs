

using System;
using Chickchilinka.Window.Interfaces;
using R3;
using UnityEngine;
using Utils;

namespace Chickchilinka.Window.Abstract
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class AbstractContent : MonoBehaviour, IContent
    {
        void ICloseListener.SubscribeOnClose(Action onClosed) => _closeAction += onClosed;
        void ICloseListener.UnsubscribeOnClose(Action onClosed) => _closeAction -= onClosed;
        void ICloseListener.ClearCloseSubscriptions() => _closeAction = null;
        private Action _closeAction;
        public string Id => GetType().Name;
        public abstract string Title { get; }
        public virtual Observable<string> TitleChanged { get; protected set; } = Observable.Never<string>();

        protected void Close() => _closeAction?.Invoke();
        public virtual void SetParent(RectTransform parent)
        {
            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            
            
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.ApplyRect(parent.rect);
        }
    }
}