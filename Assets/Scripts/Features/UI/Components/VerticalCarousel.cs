using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components
{
    /// <summary>
    /// Carousel component optimized for vertical scrolling.
    /// Expects Content to have Top Stretch anchor (0,1,1,1) and pivot (0.5,1).
    /// </summary>
    public class VerticalCarousel : CarouselBase
    {
        private VerticalLayoutGroup _verticalLayoutGroup;

        public override bool IsVertical => true;

        protected override float AxisPos(RectTransform rt) => rt.anchoredPosition.y;

        protected override float ContentAxisPos() => Content.anchoredPosition.y;

        protected override float AxisVelocityAbs()
        {
            var v = _scrollRect.velocity;
            return Mathf.Abs(v.y);
        }

        protected override Vector2 GetTargetContentPosFor(RectTransform target)
        {
            var p = _content.anchoredPosition;
            var elementPos = AxisPos(target);

            // For Top Stretch anchor, center element in viewport
            var viewportCenter = _viewport.rect.height * 0.5f;
            p.y = -elementPos - viewportCenter;

            return p;
        }

        protected override void SetPadding(int start, int end)
        {
            CacheLayoutGroup();

            if (_verticalLayoutGroup == null)
                return;

            _verticalLayoutGroup.padding.top = start;
            _verticalLayoutGroup.padding.bottom = end;

            LayoutRebuilder.MarkLayoutForRebuild(_content);
        }

        private void CacheLayoutGroup()
        {
            _verticalLayoutGroup ??= _content.GetComponent<VerticalLayoutGroup>();
        }

        protected override float GetPreferredSize(RectTransform rt)
        {
            if (rt == null) return 0f;
            var h = LayoutUtility.GetPreferredHeight(rt);
            return h > 0f ? h : rt.rect.height;
        }

        protected override float GetViewportSize()
        {
            return _viewport.rect.height;
        }

        protected override int GetNearestIndexSimple()
        {
            if (_elements.Count == 0) return 0;

            var nearest = 0;
            var bestDistance = float.MaxValue;
            var currentContentY = _content.anchoredPosition.y;

            for (var i = 0; i < _elements.Count; i++)
            {
                var targetContentY = GetTargetContentPosFor(_elements[i]).y;
                
                var distance = Mathf.Abs(targetContentY - currentContentY);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        public override float GetDistanceFromViewportCenter(RectTransform element)
        {
            if (element == null) 
                return float.MaxValue;
            
            var elementPos = AxisPos(element);
            var currentContentY = ContentAxisPos();
            var targetContentY = -elementPos - _viewport.rect.height * 0.5f;
            
            return Mathf.Abs(targetContentY - currentContentY);
        }
    }
}