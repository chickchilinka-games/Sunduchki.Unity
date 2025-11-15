namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestStateWriter
    {
        void RegisterRequest(string from, string target, string rank);

        void RegisterTransfer(string from, string to, string rank, int count);

        void RegisterNoCards(string from, string target, string rank);

        void Reset();
    }
}
