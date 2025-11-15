using Modules.DeckSystem.Data;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckStateWriter
    {
        void ConfigureCounts(int? remaining, int? total);

        void AdjustBy(int delta);

        void SetPeek(DeckPeekInfo info);

        void ClearPeek();

        void Reset();
    }
}
