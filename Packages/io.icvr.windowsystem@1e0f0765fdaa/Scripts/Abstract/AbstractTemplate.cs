

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