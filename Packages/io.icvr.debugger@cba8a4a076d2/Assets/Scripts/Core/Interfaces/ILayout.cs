using Core.Enums;
using UnityEngine;

namespace Core.Interfaces
{
    public interface ILayout : IIdentified
    {
        public LayoutType LayoutType { get; }
        public RectTransform Pivot { get; }
    }
}