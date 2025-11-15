using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.StateMachine.Substeps
{
    public interface ISubstep
    {
        UniTask<bool> ExecuteAsync(CancellationToken cancellationToken);
    }
}