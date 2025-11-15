using UnityEngine;
using UnityEngine.UI;

namespace Core.Components
{
    [RequireComponent(typeof(Button))]
    public class WidgetToggle : MonoBehaviour
    {
        [SerializeField] private GameObject widget;
        [SerializeField] private Image toggleImage;

        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite inactiveSprite;

        private void Start()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(ToggleWidget);
            UpdateToggleSprite();
        }

        public void ToggleWidget()
        {
            if (widget == null)
            {
                Debug.LogWarning("WidgetToggle: Widget reference is missing!");
                return;
            }

            widget.SetActive(!widget.activeSelf);
            UpdateToggleSprite();
        }

        private void UpdateToggleSprite()
        {
            if (toggleImage != null) toggleImage.sprite = widget.activeSelf ? activeSprite : inactiveSprite;
        }
    }
}