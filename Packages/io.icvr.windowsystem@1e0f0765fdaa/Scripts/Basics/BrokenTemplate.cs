

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using UnityEngine;

namespace ICVR.Window.Basics
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