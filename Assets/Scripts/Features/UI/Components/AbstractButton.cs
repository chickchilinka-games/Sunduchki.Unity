using System;
using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components
{
    [RequireComponent(typeof(Button))]
    public abstract class AbstractButton : MonoBehaviour
    {
        private Button _button;
        protected Button Button => _button ??= GetComponent<Button>();

        private void Awake()
        {
            Button.onClick.AddListener(OnClick);
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            
        }

        private void OnDestroy()
        {
            Button.onClick.RemoveListener(OnClick);
        }

        protected void SetInteractable(bool interactable)
        {
            Button.interactable = interactable;
        }

        protected abstract void OnClick();
    }
}