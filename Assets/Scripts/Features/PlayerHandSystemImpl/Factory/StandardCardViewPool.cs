using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.View;
using Features.PlayerHandSystemImpl.ViewModel;

namespace Features.PlayerHandSystemImpl.Factory
{
    public sealed class StandardCardViewPool : ViewPoolBase<StandardCardViewModel, StandardCardView>
    {
        protected override void OnViewReinitialize(StandardCardViewModel data, StandardCardView item)
        {
            item.Initialize(data).Forget();
        }

        protected override void OnViewDespawned(StandardCardView item)
        {
            item.ResetView();
        }
    }
}
