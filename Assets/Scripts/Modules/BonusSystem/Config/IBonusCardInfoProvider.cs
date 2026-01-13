namespace Modules.BonusSystem.Config
{
    public interface IBonusCardInfoProvider
    {
        bool TryGetInfo(string bonusType, out BonusCardInfo info);
    }
}
