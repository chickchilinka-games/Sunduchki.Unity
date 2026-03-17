

using ICVR.Window.Interfaces;
using UnityEngine;

namespace ICVR.Window.Components
{
    public class WindowHolder : MonoBehaviour, IHolder
    {
        public RectTransform Area => (RectTransform)transform;
    }
}