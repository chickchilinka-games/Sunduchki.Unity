using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.View;
using Features.PlayerHandSystemImpl.ViewModel;

namespace Features.PlayerHandSystemImpl.Factory
{
    public sealed class RankStackViewPool : ViewPoolBase<RankStackViewModel, RankStackView>
    {
        protected override void OnViewReinitialize(RankStackViewModel data, RankStackView item)
        {
            item.Initialize(data).Forget();
        }

        protected override void OnViewDespawned(RankStackView item)
        {
            item.ResetView();
        }
    }
}
