using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.UI.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public abstract class CarouselBase : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _snapDuration = 0.25f;
        [SerializeField] private Ease _ease = Ease.OutCubic;
        [SerializeField] private bool _autoEdgePadding = true;

        [Header("Elements Management")]
        [SerializeField] private bool _collectOnAwake = false;

        [Header("Velocity Snap")]
        [SerializeField] private float _snapVelocityThreshold = 100f;
        [SerializeField] private float _postDragSnapDelay = 0.03f;

        [Header("Precision")]
        [SerializeField] private float _distanceEpsilon = 0.5f;

        public event Action<int> OnIndexChanged;

        public int CurrentIndex => _currentIndex;
        public RectTransform CurrentElement =>
            _currentIndex >= 0 && _currentIndex < _elements.Count ? _elements[_currentIndex] : null;
        public IReadOnlyList<RectTransform> Elements => _elements;

        public ScrollRect Scroll => _scrollRect ??= GetComponent<ScrollRect>();
        public RectTransform Content => _content ??= Scroll.content;
        public abstract bool IsVertical { get; }

        protected ScrollRect _scrollRect;
        protected RectTransform _viewport;
        protected RectTransform _content;
        protected readonly List<RectTransform> _elements = new();

        protected Tween _scrollTween;
        protected bool _isSnapping;
        protected bool _isDragging;
        protected int _currentIndex;
        protected float _lastDragEndTime;
        private bool _initialized;

        protected virtual void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport;

            if (_collectOnAwake)
                CacheExistingElements();

            _scrollRect.OnBeginDragAsObservable()
                .Subscribe(_ =>
                {
                    _isDragging = true;
                    _scrollTween?.Kill();
                    _isSnapping = false;
                })
                .AddTo(this);

            _scrollRect.OnEndDragAsObservable()
                .Subscribe(OnEndDrag)
                .AddTo(this);

            _scrollRect.OnValueChangedAsObservable()
                .Subscribe(_ => UpdateCurrentIndexSimple())
                .AddTo(this);
        }

        protected virtual async void Start()
        {
            if (_autoEdgePadding)
                await UpdateEdgePaddingAsync();

            // Instant snap to first element on start
            if (_elements.Count > 0)
            {
                var targetPos = GetTargetContentPosFor(_elements[0]);
                _content.anchoredPosition = targetPos;
                _currentIndex = 0;
                OnIndexChanged?.Invoke(_currentIndex);
            }

            _initialized = true;
        }

        protected virtual void Update()
        {
            if(!_initialized)
                return;
            if (_elements.Count == 0 || _isSnapping) return;
            if (_isDragging || Time.unscaledTime - _lastDragEndTime < _postDragSnapDelay) return;

            var axisVel = AxisVelocityAbs();
            if (axisVel <= _snapVelocityThreshold)
                TrySnapNow();
        }

        protected void CacheExistingElements()
        {
            _elements.Clear();
            foreach (Transform child in _content)
                if (child is RectTransform rt) _elements.Add(rt);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            UpdateCurrentIndexSimple();
        }

        public async UniTask AddElement(RectTransform element)
        {
            AddElementInternal(element);

            if (_autoEdgePadding)
                await UpdateEdgePaddingAsync();

            UpdateCurrentIndexSimple();
        }

        public async UniTask AddElements(IEnumerable<RectTransform> elements)
        {
            if (elements == null) return;

            var elementsArray = elements.Where(el => el != null).ToArray();
            if (elementsArray.Length == 0) return;

            foreach (var el in elementsArray)
                AddElementInternal(el);

            if (_autoEdgePadding)
                await UpdateEdgePaddingAsync();

            UpdateCurrentIndexSimple();
        }

        private void AddElementInternal(RectTransform element)
        {
            if (element == null || _elements.Contains(element)) return;

            element.SetParent(_content, false);
            _elements.Add(element);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        public async UniTask RemoveElement(RectTransform element)
        {
            if (element == null || !_elements.Remove(element)) return;

            if (element.gameObject != null)
                Destroy(element.gameObject);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

            if (_autoEdgePadding)
                await UpdateEdgePaddingAsync();

            UpdateCurrentIndexSimple();
        }

        public async UniTask Clear()
        {
            foreach (var el in _elements.Where(el => el))
                Destroy(el.gameObject);
            _elements.Clear();
            _currentIndex = 0;
            if (_autoEdgePadding) await UpdateEdgePaddingAsync();
        }

        protected virtual void OnEndDrag(PointerEventData _)
        {
            _isDragging = false;
            _lastDragEndTime = Time.unscaledTime;
            if (AxisVelocityAbs() <= _snapVelocityThreshold)
                TrySnapNow();
        }

        protected abstract float AxisPos(RectTransform rt);
        protected abstract float ContentAxisPos();
        protected abstract float AxisVelocityAbs();
        protected abstract Vector2 GetTargetContentPosFor(RectTransform target);

        protected virtual int GetNearestIndexSimple()
        {
            if (_elements.Count == 0) return 0;
            var c = ContentAxisPos();
            var nearest = 0;
            var best = float.MaxValue;
            for (int i = 0; i < _elements.Count; i++)
            {
                var e = AxisPos(_elements[i]);
                var dist = Mathf.Abs(e + c);
                if (dist < best) { best = dist; nearest = i; }
            }
            return nearest;
        }

        protected void UpdateCurrentIndexSimple()
        {
            if (_isSnapping || _elements.Count == 0) return;
            var newIndex = GetNearestIndexSimple();
            if (newIndex == _currentIndex) return;
            _currentIndex = newIndex;
            OnIndexChanged?.Invoke(_currentIndex);
        }

        protected void TrySnapNow()
        {
            if (_elements.Count == 0 || _content == null || _scrollRect == null) return;

            var idx = GetNearestIndexSimple();
            if (idx < 0 || idx >= _elements.Count) return;

            var targetElement = _elements[idx];
            if (targetElement == null) return;

            var targetPos = GetTargetContentPosFor(targetElement);
            _scrollRect.velocity = Vector2.zero;

            var delta = Mathf.Abs(IsVertical
                ? targetPos.y - _content.anchoredPosition.y
                : targetPos.x - _content.anchoredPosition.x);

            if (delta <= _distanceEpsilon)
            {
                _content.anchoredPosition = targetPos;
                if (_currentIndex != idx)
                {
                    _currentIndex = idx;
                    OnIndexChanged?.Invoke(_currentIndex);
                }
                return;
            }

            SnapTo(idx, targetPos);
        }

        protected void SnapTo(int index, Vector2 targetPos)
        {
            if (index < 0 || index >= _elements.Count || _content == null || _scrollRect == null)
                return;

            _scrollTween?.Kill();
            _isSnapping = true;
            _scrollRect.velocity = Vector2.zero;

            _scrollTween = _content.DOAnchorPos(targetPos, _snapDuration)
                .SetEase(_ease)
                .OnComplete(() =>
                {
                    if (_content != null)
                        _content.anchoredPosition = targetPos;

                    _currentIndex = index;
                    OnIndexChanged?.Invoke(_currentIndex);
                    _isSnapping = false;

                    if (_scrollRect != null)
                        _scrollRect.velocity = Vector2.zero;
                });
        }

        public async UniTask ScrollToIndex(int index)
        {
            if (index < 0 || index >= _elements.Count || _content == null || _scrollRect == null)
                return;

            var targetElement = _elements[index];
            if (targetElement == null) return;

            var targetPos = GetTargetContentPosFor(targetElement);

            _scrollTween?.Kill();
            _isSnapping = true;
            _scrollRect.velocity = Vector2.zero;

            _scrollTween = _content.DOAnchorPos(targetPos, _snapDuration).SetEase(_ease);

            try
            {
                await _scrollTween.AsyncWaitForCompletion();
            }
            catch
            {
                // Tween was killed or failed, handle gracefully
                _isSnapping = false;
                return;
            }

            if (_content != null)
                _content.anchoredPosition = targetPos;

            _currentIndex = index;
            OnIndexChanged?.Invoke(_currentIndex);
            _isSnapping = false;

            if (_scrollRect != null)
                _scrollRect.velocity = Vector2.zero;
        }

        // Edge padding functionality
        protected abstract void SetPadding(int start, int end);
        protected abstract float GetPreferredSize(RectTransform rt);
        protected abstract float GetViewportSize();


        public async UniTask UpdateEdgePaddingAsync()
        {
            if (!_autoEdgePadding || _viewport == null || _content == null) return;
            if (Elements.Count == 0) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            await UniTask.NextFrame();

            var vpSize = GetViewportSize();
            var first = Elements.FirstOrDefault();
            var last = Elements.LastOrDefault();

            if (first == null || last == null) return;

            var startPadding = Mathf.Max(0, Mathf.RoundToInt((vpSize - GetPreferredSize(first)) * 0.5f));
            var endPadding = Mathf.Max(0, Mathf.RoundToInt((vpSize - GetPreferredSize(last)) * 0.5f));

            SetPadding(startPadding, endPadding);

            await UniTask.NextFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        /// <summary>
        /// Calculate distance of an element from viewport center.
        /// Useful for animations, effects, and visual feedback.
        /// </summary>
        /// <param name="element">Element to calculate distance for</param>
        /// <returns>Distance from viewport center (0 = centered)</returns>
        public virtual float GetDistanceFromViewportCenter(RectTransform element)
        {
            if (element == null) return float.MaxValue;

            // Default implementation using base algorithm (works for HorizontalCarousel)
            var elementPos = AxisPos(element);
            var contentPos = ContentAxisPos();
            return Mathf.Abs(elementPos + contentPos);
        }

        [ContextMenu("Rebuild Elements")]
        public void RebuildElements() => CacheExistingElements();
    }
}
