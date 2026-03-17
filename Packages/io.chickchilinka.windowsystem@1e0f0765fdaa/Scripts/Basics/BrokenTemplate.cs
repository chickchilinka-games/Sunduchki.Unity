

using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Abstract;
using UnityEngine;

namespace Chickchilinka.Window.Basics
{
    internal class BrokenTemplate : AbstractTemplate
    {
        public override UniTask Show()
        {
            return UniTask.CompletedTask;
        }

        public override UniTask Hide()
        {
            return UniTask.CompletedTask;
        }
    }
}