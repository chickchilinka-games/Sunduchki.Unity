namespace Features.PlayerHandSystemImpl.Presentation.ViewModel
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
