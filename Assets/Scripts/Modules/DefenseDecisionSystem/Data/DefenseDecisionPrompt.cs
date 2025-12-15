using System;
using System.Collections.Generic;

namespace Modules.DefenseDecisionSystem.Data
{
    public sealed class DefenseDecisionPrompt
    {
        public string AskerId { get; }
        public string TargetId { get; }
        public string Rank { get; }
        public IReadOnlyList<string> DefenseOptions { get; }

        public DefenseDecisionPrompt(string askerId, string targetId, string rank, IReadOnlyList<string> defenseOptions)
        {
            AskerId = askerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Rank = rank ?? string.Empty;
            DefenseOptions = defenseOptions ?? Array.Empty<string>();
        }
    }
}
