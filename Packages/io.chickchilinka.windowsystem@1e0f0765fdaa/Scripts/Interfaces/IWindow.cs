

using System;
using Cysharp.Threading.Tasks;

namespace Chickchilinka.Window.Interfaces
{
    internal interface IWindow : IIdentified, IChild, IDisposable
    {
        bool IsShown { get; }
        internal IContent Content { get; }
        internal ITemplate Template { get; }
        UniTask Show();
        UniTask Hide();
    }
}