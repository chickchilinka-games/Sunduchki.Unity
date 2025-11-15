using DebuggerPlugins.DataView.Core.Interfaces;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace DebuggerPlugins.DataView.Core.Views
{
    internal class StringDataView : MonoBehaviour, IDataViewLayout
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _content;
        [SerializeField] private RectTransform _pivot;

        public RectTransform Pivot => _pivot;

        [Inject]
        private void Construct(IStringData stringData)
        {
            stringData.Content.Subscribe(
                newContent => _content.text = newContent)
                .AddTo(this);

            _title.text = stringData.Title;
            _content.text = stringData.Content.CurrentValue;
        }
    }
}