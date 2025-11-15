using System;
using System.Linq;
using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Extensions;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace DebuggerPlugins.Logger.View
{
    internal class MessageView : MonoBehaviour, IDisposable, IPoolable<LoggerMessage, IMemoryPool>
    {
        [SerializeField] private TMP_Text logText;
        [SerializeField] private Button clickArea;
        [SerializeField] private RectTransform collapsedPanel;
        [SerializeField] private TMP_Text collapsedText;

        public UnityEvent Clicked { get; private set; }
        public LoggerMessage Data { get; private set; }

        public ReactiveProperty<int> CollapsedCounter { get; private set; }

        private IMemoryPool _pool;
        private LoggerViewConfig _config;
        private Subject<Unit> _disposed;

        [Inject]
        public void Construct(LoggerViewConfig config)
        {
            _config = config;
            Clicked = clickArea.onClick;
            CollapsedCounter = new ReactiveProperty<int>(1);
            CollapsedCounter.Subscribe(UpdateCounterText).AddTo(this);
            DisplayCollapsedCounter(false);
        }

        private void Initialize(LoggerMessage data)
        {
            Data = data;
            logText.text = data.ToString();
            logText.color = _config.logColors.First(logColorInfo => logColorInfo.LogType == data.LogType).Color;
        }

        public void Dispose()
        {
            Data = default;
            Clicked.RemoveAllListeners();
            CollapsedCounter.Value = 1;
            DisplayCollapsedCounter(false);
            _pool.Despawn(this);
        }

        public void OnSpawned(LoggerMessage data, IMemoryPool pool)
        {
            _pool = pool;
            Initialize(data);
        }

        public void OnDespawned()
        {
            _pool = null;
        }

        public void DisplayCollapsedCounter(bool isVisible)
        {
            collapsedPanel.gameObject.SetActive(isVisible);

            if (isVisible)
            {
                collapsedText.ForceRebuild();
            }
        }

        private void UpdateCounterText(int value)
        {
            collapsedText.text = value.ToString();
            collapsedText.ForceRebuild();
        }
    }
}