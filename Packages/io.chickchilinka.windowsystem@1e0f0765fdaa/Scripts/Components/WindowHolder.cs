

using Chickchilinka.Window.Interfaces;
using UnityEngine;

namespace Chickchilinka.Window.Components
{
    public class WindowHolder : MonoBehaviour, IHolder
    {
        public RectTransform Area => (RectTransform)transform;
    }
}