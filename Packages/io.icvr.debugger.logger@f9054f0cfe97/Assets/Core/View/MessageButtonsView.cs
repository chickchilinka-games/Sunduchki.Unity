using System;
using DebuggerPlugins.Logger.Data;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace DebuggerPlugins.Logger.View
{
    internal class MessageButtonsView : MonoBehaviour
    {
        [SerializeField] private RectTransform messagePopup;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button showFullButton;
        [SerializeField] private Button copyAllButton;
        [SerializeField] private Button saveAllButton;

        [SerializeField] private RectTransform fullMessagePanel;
        [SerializeField] private TMP_Text fullMessageText;

        public Observable<LoggerMessage> Copy => copyButton.OnClickAsObservable().Select(_ => _current.Data);
        public Observable<Unit> CopyAll => copyAllButton.OnClickAsObservable();
        public Observable<Unit> SaveAll => saveAllButton.OnClickAsObservable();

        private bool IsShown => messagePopup.gameObject.activeSelf;

        private MessageView _current;

        private void Start()
        {
            ResetPopup();
            ToggleFullMessagePanel(false);
            showFullButton.onClick.AddListener(() => ToggleFullMessagePanel(true));
        }

        public void OnMessageClicked(MessageView target)
        {
            bool isNewTarget = target != _current;
            if (isNewTarget)
            {
                messagePopup.SetParent(target.transform, false);
            }

            messagePopup.gameObject.SetActive(isNewTarget || !IsShown);
            _current = target;
        }

        public void OnMessageDisposed(MessageView target)
        {
            if (target == _current)
            {
                ResetPopup();
            }
        }

        private void ResetPopup()
        {
            _current = null;
            messagePopup.SetParent(transform, false);
            messagePopup.gameObject.SetActive(false);
        }

        public void ToggleFullMessagePanel(bool active)
        {
            fullMessagePanel.gameObject.SetActive(active);
            if (active && _current != null)
                fullMessageText.text = _current.Data.ToStringExtended();
        }
    }
}