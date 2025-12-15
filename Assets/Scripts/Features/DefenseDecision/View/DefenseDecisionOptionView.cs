using System;
using UnityEngine;
using UnityEngine.UI;

namespace Features.DefenseDecision.View
{
    public sealed class DefenseDecisionOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        private Action _callback;

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void OnDestroy()
        {
            ResetView();
        }

        public void Initialize(string label, Action callback)
        {
            _callback = callback;
            if (_label != null)
            {
                _label.text = label ?? string.Empty;
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClicked);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void ResetView()
        {
            _callback = null;
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = true;
            }

            if (_label != null)
            {
                _label.text = string.Empty;
            }
        }

        private void OnClicked()
        {
            _callback?.Invoke();
        }
    }
}
