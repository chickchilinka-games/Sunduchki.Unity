using ObservableCollections;
using R3;
using R3.Collections;

namespace DebuggerPlugins.Logger.Storages
{
    internal class LoggerCategoriesStorage
    {
        private readonly ObservableList<string> _reactiveCollection;

        public IReadOnlyObservableList<string> Items => _reactiveCollection;

        public LoggerCategoriesStorage()
        {
            _reactiveCollection = new ObservableList<string>();
        }

        public void Add(string data)
        {
            if (_reactiveCollection.Contains(data))
            {
                return;
            }

            _reactiveCollection.Add(data);
        }
    }
}