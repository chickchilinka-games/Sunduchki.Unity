using System;
using System.Threading.Tasks;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DebuggerPlugins.Logger.View
{
    [Serializable]
    internal class MessageScrollRect : IDisposable
    {
        [SerializeField] private RectTransform messagesContainer;
        [SerializeField] private RectTransform messagesViewport;
        [SerializeField] private ScrollRect scrollRect;

        private const float LoadRelativePosition = 0.05f;
        private const float StopFollowRelativePosition = 0.3f;

        public RectTransform MessagesContainer => messagesContainer;
        public bool IsOutOfFollowRange => NormalizedPosition > StopFollowRelativePosition;
        public Observable<Unit> LoadUp => _loadUp;
        private readonly Subject<Unit> _loadUp = new();
        public Observable<Unit> LoadDown => _loadDown;
        private readonly Subject<Unit> _loadDown = new();

        public float NormalizedPosition
        {
            get => scrollRect.verticalNormalizedPosition / GetScrollRectScaleFactor();
            set
            {
                var pos = value * GetScrollRectScaleFactor();

                _isScrolling = true;
                
                var wasDragging = _isDragging;

                if (wasDragging)
                {
                    scrollRect.OnEndDrag(_lastDragEventData);
                }

                var velocity = scrollRect.velocity;
                scrollRect.verticalNormalizedPosition = pos;
                scrollRect.velocity = velocity;

                if (wasDragging)
                {
                    scrollRect.OnBeginDrag(_lastDragEventData);
                }

                Observable.NextFrame().Subscribe(_ => _isScrolling = false).AddTo(_disposables);
            }
        }

        private bool IsAtTheBottom => NormalizedPosition <= 0.01f;

        private bool _isDragging;
        private PointerEventData _lastDragEventData;
        private bool _isAutoScrolling;
        private bool _isScrolling;

        private CompositeDisposable _disposables;
        
        public void Init()
        {
            _disposables = new CompositeDisposable();

            scrollRect.verticalNormalizedPosition = 0;
            scrollRect.onValueChanged.AsObservable().Subscribe(pos => OnScroll(pos.y)).AddTo(_disposables);
            scrollRect.OnDragAsObservable().Subscribe(value => _lastDragEventData = value).AddTo(_disposables);
            scrollRect.OnBeginDragAsObservable().Subscribe(_ => _isDragging = true).AddTo(_disposables);
            scrollRect.OnEndDragAsObservable().Subscribe(_ => _isDragging = false).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        private void OnScroll(float pos)
        {
            if (_isScrolling)
            {
                return;
            }

            if (pos > 1 - LoadRelativePosition)
            {
                _loadUp.OnNext(new Unit());
            }
            else if (pos < LoadRelativePosition)
            {
                _loadDown.OnNext(new Unit());
            }
        }

        public async void AutoScroll(float normalizedOffset)
        {
            if (!IsAtTheBottom)
            {
                NormalizedPosition += normalizedOffset;
                return;
            }

            if (_isAutoScrolling)
            {
                return;
            }

            _isAutoScrolling = IsAtTheBottom;

            if (!_isAutoScrolling)
            {
                return;
            }
            
            await Task.Yield();
                
            scrollRect.verticalNormalizedPosition = 0;
                
            _isAutoScrolling = false;
        }

        private float GetScrollRectScaleFactor()
        {
            var containerHeight = messagesContainer.rect.height;
            var viewportHeight = messagesViewport.rect.height;
            var scrollRectScaleFactor = containerHeight / (containerHeight - viewportHeight);
            
            return scrollRectScaleFactor;
        }
    }
}