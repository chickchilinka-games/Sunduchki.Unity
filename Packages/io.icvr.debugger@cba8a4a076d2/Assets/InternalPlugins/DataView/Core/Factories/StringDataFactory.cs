using DebuggerPlugins.DataView.Core.Interfaces;
using DebuggerPlugins.DataView.Core.Views;
using Zenject;

namespace DebuggerPlugins.DataView.Core.Factories
{
    internal class StringDataFactory : PlaceholderFactory<IStringData, StringDataView>
    {
    }
}