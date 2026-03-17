

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Chickchilinka.Window.Interfaces
{
    internal interface ITemplate : IIdentified, IChild, ICloseListener
    {
        RectTransform FrameRect { get; }
        void SetContent(IContent content);
        UniTask Show();
        UniTask Hide();
    }
}