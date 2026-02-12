using System;
using Features.UI.Components;
using Modules.AuthenticationSystem.Data;
using TMPro;
using UnityEngine;

namespace Features.AuthenticationSystemImpl.Views
{
    public class LinkAccountButton : AbstractButton
    {
        [field: SerializeField] 
        public AuthType AuthType { get; private set; }
        [SerializeField] private TMP_Text _usernameTMP;
        [SerializeField] private TMP_Text _authTypeTMP;
        [SerializeField] private GameObject _isLinkedView;

        private Action<AuthType> _onClick;

        protected override void OnAwake()
        {
            _authTypeTMP.text = AuthType.ToString();
        }
        
        public void Initialize(bool isEnabled, Action<AuthType> onClick, bool isLinked, LinkageInfo linkageInfo = null)
        {
            _onClick = onClick;
            gameObject.SetActive(isEnabled);
            
            _isLinkedView.gameObject.SetActive(isLinked);
            _usernameTMP.gameObject.SetActive(isLinked);
            if(!isLinked)
                return;
            
            _usernameTMP.text = linkageInfo?.Username;
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
