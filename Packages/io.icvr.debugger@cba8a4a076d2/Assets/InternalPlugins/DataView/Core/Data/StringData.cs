using DebuggerPlugins.DataView.Core.Interfaces;
using R3;

namespace DebuggerPlugins.TestDataView.Core.Data
{
    public class StringData : IStringData
    {
        protected readonly ReactiveProperty<string> InnerContent;
        public ReadOnlyReactiveProperty <string> Content => InnerContent;
        public string Title { get; }

        public StringData(string title)
        {
            Title = title;
            InnerContent = new ReactiveProperty<string>(string.Empty);
        }

        public StringData(string title, string data)
        {
            Title = title;
            InnerContent = new ReactiveProperty<string>(data);
        }
    }
}