using Modules.DefenseDecisionSystem.Data;

namespace Modules.DefenseDecisionSystem.Interfaces
{
    public interface IDefenseDecisionPromptWriter
    {
        void PublishPrompt(DefenseDecisionPrompt prompt);
        void ClearPrompt();
    }
}
