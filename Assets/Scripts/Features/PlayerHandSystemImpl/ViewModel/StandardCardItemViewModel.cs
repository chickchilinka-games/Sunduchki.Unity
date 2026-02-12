namespace Features.PlayerHandSystemImpl.ViewModel
{
    public sealed class StandardCardItemViewModel
    {
        public string Suit { get; }

        public StandardCardItemViewModel(string suit)
        {
            Suit = suit ?? string.Empty;
        }
    }
}
