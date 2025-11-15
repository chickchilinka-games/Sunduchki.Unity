using Cysharp.Threading.Tasks;

namespace Cheats.Core.Interfaces
{
    public interface ICheatAction : ICheat
    {
        public UniTask<bool> Execute();
    }
}