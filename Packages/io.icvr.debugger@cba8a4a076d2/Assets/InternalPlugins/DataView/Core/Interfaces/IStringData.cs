using System;
using R3;

namespace DebuggerPlugins.DataView.Core.Interfaces
{
    public interface IStringData
    {
        public ReadOnlyReactiveProperty<string> Content { get; }
        public string Title { get; }
    }
}