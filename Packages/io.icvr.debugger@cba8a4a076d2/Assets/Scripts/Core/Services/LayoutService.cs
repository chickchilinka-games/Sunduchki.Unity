using System.Collections.Generic;
using Core.Factories;
using Core.Interfaces;

namespace Core.Services
{
    internal class LayoutService
    {
        private readonly LayoutResolver _layoutResolver;

        public LayoutService(LayoutResolver layoutResolver)
        {
            _layoutResolver = layoutResolver;
        }

        public List<ILayout> GetLayouts()
        {
            return _layoutResolver.Resolve();
        }
    }
}