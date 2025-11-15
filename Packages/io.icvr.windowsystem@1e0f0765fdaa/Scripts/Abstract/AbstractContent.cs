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
using ICVR.Window.Interfaces;
using R3;
using UnityEngine;
using Utils;

namespace ICVR.Window.Abstract
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