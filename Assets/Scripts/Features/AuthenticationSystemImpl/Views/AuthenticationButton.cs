using System;
using Features.UI.Components;
using Modules.AuthenticationSystem.Data;
using UnityEngine;

namespace Features.AuthenticationSystemImpl.Views
{
    public class AuthenticationButton: AbstractButton
    {
        [field: SerializeField]
        public AuthType AuthType { get; private set; }
        
        private Action<AuthType> _onClick;

        public void Initialize(bool isEnabled, Action<AuthType> onClick)
        {
            gameObject.SetActive(isEnabled);
            _onClick = onClick;
        }
        
        protected override void OnClick()
        {
            _onClick?.Invoke(AuthType);
        }

        private void OnDestroy()
        {
            _onClick = null;
        }
    }
}