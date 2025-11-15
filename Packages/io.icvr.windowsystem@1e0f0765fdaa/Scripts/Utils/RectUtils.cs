// // ICVR CONFIDENTIAL
// // __________________
// //
// // [2016] - [2024] ICVR LLC
// // All Rights Reserved.
// //
// // NOTICE:  All information contained herein is, and remains
// // the property of ICVR LLC and its suppliers,
// // if any.  The intellectual and technical concepts contained
// // herein are proprietary to ICVR LLC
// // and its suppliers and may be covered by U.S. and Foreign Patents,
// // patents in process, and are protected by trade secret or copyright law.
// // Dissemination of this information or reproduction of this material
// // is strictly forbidden unless prior written permission is obtained
// // from ICVR LLC.

using UnityEngine;

namespace Utils
{
    public static class RectUtils
    {
        public static void ApplyRect(this RectTransform rectTransform, Rect rect)
        {
            rectTransform.rect.Set(rect.x, rect.y, rect.width, rect.height);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}