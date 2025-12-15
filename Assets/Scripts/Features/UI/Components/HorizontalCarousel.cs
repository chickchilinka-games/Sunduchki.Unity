using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components
{
    /// <summary>
    /// Carousel component optimized for horizontal scrolling.
    /// Expects Content RectTransform to have pivot.x = 0 for proper behavior.
    /// </summary>
    public class HorizontalCarousel : CarouselBase
    {
        private HorizontalLayoutGroup _horizontalLayoutGroup;
        private GridLayoutGroup _gridLayoutGroup;

        public override bool IsVertical => false;

        protected override float AxisPos(RectTransform rt) => rt.anchoredPosition.x;

        protected override float ContentAxisPos() => Content.anchoredPosition.x;

        protected override float AxisVelocityAbs()
        {
            var v = _scrollRect.velocity;
            return Mathf.Abs(v.x);
        }

        protected override Vector2 GetTargetContentPosFor(RectTransform target)
        {
            var p = _content.anchoredPosition;
            var elementPos = AxisPos(target);
            
            p.x = -elementPos;

            return p;
        }

        protected override void SetPadding(int start, int end)
        {
            CacheLayoutGroup();

            if (_horizontalLayoutGroup == null)
                return;
            
            _horizontalLayoutGroup.padding.left = start;
            _horizontalLayoutGroup.padding.right = end;

            LayoutRebuilder.MarkLayoutForRebuild(_content);
        }

        private void CacheLayoutGroup()
        {
            _horizontalLayoutGroup ??= _content.GetComponent<HorizontalLayoutGroup>();
        }

        protected override float GetPreferredSize(RectTransform rt)
        {
            if (rt == null) return 0f;
            var w = LayoutUtility.GetPreferredWidth(rt);
            return w > 0f ? w : rt.rect.width;
        }

        protected override float GetViewportSize()
        {
            return _viewport.rect.width;
        }
    }
}