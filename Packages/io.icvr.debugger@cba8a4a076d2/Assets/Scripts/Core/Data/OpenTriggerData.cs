using System;
using UnityEngine;

namespace Core.Data
{
    [Serializable]
    public class OpenTriggerData
    {
        [Tooltip("Tap trigger position (in viewport coordinates). Default is (0,0) - bottom left).")]
        public Vector2 Position = new(0,0);
        [Tooltip("Size of trigger (in pixel coordinates). Default is 100 px square.")]
        public Vector2Int PixelSize = new(100,100);
        [Tooltip("Offset of trigger (in pixel coordinates). The trigger anchor in the center," +
                 " so we have to move from the corner on the half of the size.")]
        public Vector2Int PixelOffset = new(50, 50);
        [Tooltip("Amount of taps to open the debugger.")]
        public int TapsCount = 3;
    }
}