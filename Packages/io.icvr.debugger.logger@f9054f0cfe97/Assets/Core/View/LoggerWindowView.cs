using Core.Enums;
using Core.Interfaces;
using DebuggerPlugins.Logger.Managers;
using DebuggerPlugins.Logger.Storages;
using R3;
using UnityEngine;
using Zenject;

namespace DebuggerPlugins.Logger.View
{
    internal class LoggerWindowView : MonoBehaviour, ILayout
    {
        [Header("ILayout fields")]
        [SerializeField] private string id;
        [SerializeField] private LayoutType layoutType;
        [SerializeField] private RectTransform pivot;

        [Header("Panels")]
        [SerializeField] private MessageListView messageListView;
        [SerializeField] private MessageButtonsView messageButtonsView;

        public string Id
        {
            get => id;
            protected set => id = value;
        }

        public LayoutType LayoutType
        {
            get => layoutType;
            protected set => layoutType = value;
        }

        public RectTransform Pivot
        {
            get => pivot;
            protected set => pivot = value;
        }

        private SaveDataManager _saveDataManager;
        private LoggerMessagesStorage _messagesStorage;

        [Inject]
        public void Construct(SaveDataManager saveDataManager, LoggerMessagesStorage messagesStorage)
        {
            _saveDataManager = saveDataManager;
            _messagesStorage = messagesStorage;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            messageListView.MessageClicked
                .Subscribe(messageButtonsView.OnMessageClicked)
                .AddTo(messageButtonsView);

            messageListView.MessageDisposed
                .Subscribe(messageButtonsView.OnMessageDisposed)
                .AddTo(messageButtonsView);

            messageButtonsView.Copy
                .Subscribe(message => _saveDataManager.CopyToClipboard(message.ToStringExtended()))
                .AddTo(this);

            messageButtonsView.CopyAll
                .Subscribe(_ => _saveDataManager.CopyToClipboard(_messagesStorage.ToString()))
                .AddTo(this);

            messageButtonsView.SaveAll
                .Subscribe(SaveAllLogs)
                .AddTo(this);
        }

        private void SaveAllLogs(Unit obj)
        {
            string result = _messagesStorage.ToString();
            _saveDataManager.SaveToFile(result);
        }
    }
}