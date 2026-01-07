using System.Collections.Generic;

namespace Modules.BonusSystem.Config
{
    public interface IBonusCardRulesProvider
    {
        IReadOnlyList<string> GetAttackBonusTypes();
        IReadOnlyList<string> GetDefenseBonusTypes();
    }
}
