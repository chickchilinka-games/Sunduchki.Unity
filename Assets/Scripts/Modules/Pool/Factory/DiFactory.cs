using Zenject;

namespace Base.Pool.Factory
{
    public class DiFactory
    {
        private readonly DiContainer _container;

        public DiFactory(DiContainer container)
        {
            _container = container;
        }

        public TType Create<TType>()
        {
            return _container.Resolve<TType>();
        }
    }
}