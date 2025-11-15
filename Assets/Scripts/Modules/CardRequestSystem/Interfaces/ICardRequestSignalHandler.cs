namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestSignalHandler : ICardRequestSignalListener
    {
        void ResetState();
    }
}
