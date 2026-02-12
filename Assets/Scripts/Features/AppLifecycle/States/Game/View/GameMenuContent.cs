using System;
using Features.AppLifecycle.Services;
using ICVR.Window.Abstract;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.AppLifecycle.States.Game.View
{
    public class GameMenuContent : AbstractContent
    {
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _closeButton;

        private GameMenuFlowService _menuFlowService;

        public override string Title => "Game Menu";

        [Inject]
        public void Construct(GameMenuFlowService menuFlowService)
        {
            _menuFlowService = menuFlowService ?? throw new ArgumentNullException(nameof(menuFlowService));
        }

        private void Awake()
        {
            _exitButton.onClick.AddListener(OnExitClicked);
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }
            _closeButton.onClick.RemoveListener(Close);
        }

        private void OnExitClicked()
        {
            Close();
            _closeButton.onClick.RemoveListener(Close);
            _menuFlowService?.RequestExit();
        }
    }
}
