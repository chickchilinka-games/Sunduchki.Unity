using UnityEngine;

namespace Features.UI.Components
{
    public class SafeAreaView : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = GetSafeArea();

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }

        private Rect GetSafeArea()
        {
#if UNITY_EDITOR
            Rect rect;

            if (Screen.width == 1125 && Screen.height == 2436)
            {
                rect = new Rect(0f, 102f / 2436f, 1f, 2202f / 2436f);
                return new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width,
                    Screen.height * rect.height);
            }

            if (Screen.width % 402 == 0 && Screen.height % 874 == 0)
            {
                rect = new Rect(0f, 34f/874f, 1f, 778f / 874f);
                return new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width,
                    Screen.height * rect.height);
            }

            if (Screen.width == 2436 && Screen.height == 1125)
            {
                rect = new Rect(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f);
                return new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width,
                    Screen.height * rect.height);
            }
#endif
            return Screen.safeArea;
        }
    }
}
