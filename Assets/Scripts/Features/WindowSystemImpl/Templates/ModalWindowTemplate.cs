using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;

namespace Features.WindowSystemImpl.Templates
{
    public class ModalWindowTemplate: AbstractTemplate
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
