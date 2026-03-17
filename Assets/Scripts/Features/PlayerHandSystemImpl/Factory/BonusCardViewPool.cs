using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Features.PlayerHandSystemImpl.View;

namespace Features.PlayerHandSystemImpl.Factory
{
    public sealed class BonusCardViewPool : ViewPoolBase<BonusCardViewModel, BonusCardView>
    {
        protected override void OnViewReinitialize(BonusCardViewModel data, BonusCardView item)
        {
            item.Initialize(data).Forget();
        }

        protected override void OnViewDespawned(BonusCardView item)
        {
            item.ResetView();
        }
    }
}
