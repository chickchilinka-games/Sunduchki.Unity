using System.Collections.Generic;
using DebuggerPlugins.DataView.Core.Factories;
using DebuggerPlugins.DataView.Core.Interfaces;
using InternalPlugins.DataView.Core.Storages;

namespace DebuggerPlugins.DataView.Core.Services
{
    internal class DataViewService
    {
        private readonly LayoutsStorage _layoutsStorage;
        private readonly StringDataFactory _stringDataFactory;

        public DataViewService(LayoutsStorage layoutsStorage, StringDataFactory stringDataFactory)
        {
            _layoutsStorage = layoutsStorage;
            _stringDataFactory = stringDataFactory;
        }

        public IEnumerable<IDataViewLayout> GetLayouts()
        {
            return _layoutsStorage.GetAll();
        }

        public void AddStringData(IStringData stringData)
        {
            var stringDataView = GetStringView(stringData);

            _layoutsStorage.Add(stringDataView);
        }

        private IDataViewLayout GetStringView(IStringData stringData)
        {
            return _stringDataFactory.Create(stringData);
        }
    }
}