using System.Collections.Generic;
using UnityEngine;

namespace Modules.BonusSystem.Config
{
    [CreateAssetMenu(menuName = "Sunduchki/Bonus/Bonus Card Rules", fileName = "BonusCardRules")]
    public class BonusCardRulesConfigAsset : ScriptableObject, IBonusCardRulesProvider
    {
        [SerializeField] private string[] _attackBonusTypes = { };
        [SerializeField] private string[] _defenseBonusTypes = { };

        public IReadOnlyList<string> GetAttackBonusTypes()
        {
            return _attackBonusTypes ?? System.Array.Empty<string>();
        }

        public IReadOnlyList<string> GetDefenseBonusTypes()
        {
            return _defenseBonusTypes ?? System.Array.Empty<string>();
        }

        public void SetDefaults(IEnumerable<string> attack, IEnumerable<string> defense)
        {
            _attackBonusTypes = attack == null ? System.Array.Empty<string>() : new List<string>(attack).ToArray();
            _defenseBonusTypes = defense == null ? System.Array.Empty<string>() : new List<string>(defense).ToArray();
        }
    }
}
