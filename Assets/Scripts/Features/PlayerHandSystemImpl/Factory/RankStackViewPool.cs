using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.View;
using Features.PlayerHandSystemImpl.ViewModel;

namespace Features.PlayerHandSystemImpl.Factory
{
    public sealed class RankStackViewPool : ViewPoolBase<StandardCardViewModel, RankStackView>
    {
        protected override void OnViewReinitialize(StandardCardViewModel data, RankStackView item)
        {
            item.Initialize(data).Forget();
        }

        protected override void OnViewDespawned(RankStackView item)
        {
            item.ResetView();
        }
    }
}
