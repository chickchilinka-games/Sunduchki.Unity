using System.Collections.Generic;
using System.Linq;

namespace InternalPlugins.DataView.Core.Storages
{
    internal class Storage<T>
    {
        private readonly List<T> _elements = new();

        public void Add(IEnumerable<T> reporterLayouts)
        {
            _elements.AddRange(reporterLayouts);
        }

        public void Add(T reporterLayout)
        {
            _elements.Add(reporterLayout);
        }

        public IEnumerable<T> GetAll()
        {
            return _elements.ToList();
        }
    }
}