namespace Modules.DeckSystem.Data
{
    public readonly struct DeckAdjustedEvent
    {
        public int Delta { get; }

        public DeckAdjustedEvent(int delta)
        {
            Delta = delta;
        }
    }
}
