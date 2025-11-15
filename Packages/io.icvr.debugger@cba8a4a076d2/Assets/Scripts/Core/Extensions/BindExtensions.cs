using Core.Rules;
using Zenject;

namespace Core.Extensions
{
    internal static class BindExtensions
    {
        public static void BindRule<TRule>(this DiContainer container, params object[] args) where TRule : IRule
        {
            var binder = container.BindInterfacesTo<TRule>().AsSingle();
            if (args.Length > 0) 
                binder.WithArguments(args);
            binder.NonLazy();
        }
    }
}