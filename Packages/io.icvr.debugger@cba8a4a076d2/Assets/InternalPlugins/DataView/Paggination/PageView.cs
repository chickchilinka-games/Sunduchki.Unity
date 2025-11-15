using UnityEngine;
using UnityEngine.UI;

namespace Assets.InternalPlugins.DataView.Paggination
{
    public class PageView : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private VerticalLayoutGroup _verticalLayoutGroup;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void AddContent(RectTransform content)
        {
            content.SetParent(_rectTransform);
            content.gameObject.AddComponent<PageElementView>();
            content.localScale = Vector3.one;
            _verticalLayoutGroup.CalculateLayoutInputVertical();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }

        public float GetFreeHeight()
        {
            var children = gameObject.GetComponentsInChildren<PageElementView>();
            float size = 0;
            var maxSize = _rectTransform.rect.size.y - (_verticalLayoutGroup.padding.bottom + _verticalLayoutGroup.padding.top);
            foreach (var child in children)
            {
                var rect = child.GetComponent<RectTransform>();
                size += rect.sizeDelta.y;
            }
            return maxSize - size;
        }
    }
}