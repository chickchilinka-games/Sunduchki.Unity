using ICVR.Window.Abstract;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.BonusSystemImpl.View
{
    public class BonusCardInfoContent : AbstractContentWithData<BonusCardInfoContentData>
    {
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private Image _icon;
        [SerializeField] private Button _closeButton;

        public override string Title => string.IsNullOrWhiteSpace(Data.Title) ? "Bonus Info" : Data.Title;

        private void Awake()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = Data.Title ?? string.Empty;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = Data.Description ?? string.Empty;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_icon != null)
            {
                _icon.sprite = Data.Icon;
                _icon.enabled = _icon.sprite != null;
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }
        }
    }
}
