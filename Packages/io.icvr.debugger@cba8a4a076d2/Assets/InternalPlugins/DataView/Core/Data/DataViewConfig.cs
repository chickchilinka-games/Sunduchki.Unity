using DebuggerPlugins.DataView.Core.Views;
using UnityEngine;

namespace DebuggerPlugins.DataView.Core.Data
{
    [CreateAssetMenu(menuName = "Debugger Plugins/DataView Plugin Config")]
    internal class DataViewConfig: ScriptableObject
    {
        [SerializeField] private DataWindowView _dataWindowViewPrefab;
        [SerializeField] private StringDataView _stringDataViewPrefab;

        public DataWindowView DataWindowViewPrefab => _dataWindowViewPrefab;
        public StringDataView StringDataViewPrefab => _stringDataViewPrefab;
    }
}