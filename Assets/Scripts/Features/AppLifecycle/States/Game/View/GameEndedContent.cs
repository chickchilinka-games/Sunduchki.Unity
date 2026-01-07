using System;
using Features.AppLifecycle.Services;
using ICVR.Window.Abstract;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.AppLifecycle.States.Game.View
{
    public class GameEndedContent : AbstractContentWithData<GameEndedContentData>
    {
        [SerializeField] private TMP_Text _resultLabel;
        [SerializeField] private TMP_Text _reasonLabel;
        [SerializeField] private Button _exitButton;

        private GameResultsFlowService _resultsFlowService;

        public override string Title => "Game Ended";

        [Inject]
        public void Construct(GameResultsFlowService resultsFlowService)
        {
            _resultsFlowService = resultsFlowService ?? throw new ArgumentNullException(nameof(resultsFlowService));
        }

        private void Awake()
        {
            if (_resultLabel != null)
            {
                _resultLabel.text = Data.IsWin ? "You win!" : "You lose";
            }

            if (_reasonLabel != null)
            {
                _reasonLabel.text = Data.Reason ?? string.Empty;
            }
            else if (_resultLabel != null && !string.IsNullOrWhiteSpace(Data.Reason))
            {
                _resultLabel.text = $"{_resultLabel.text}\n{Data.Reason}";
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void OnDestroy()
        {
            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        private void OnExitClicked()
        {
            _resultsFlowService?.RequestExit();
        }

    }
}
