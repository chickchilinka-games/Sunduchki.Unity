namespace Core.Interfaces
{
    public interface IPlugin
    {
        public string Title { get; }

        public void Initialize();
        public void Dispose();
    }
}