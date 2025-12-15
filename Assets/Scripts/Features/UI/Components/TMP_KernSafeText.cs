using TMPro;
using UnityEngine;

namespace Features.UI.Components
{
    /// <summary>
    /// TextMeshPro text component with additional padding to prevent text wrapping bug cause of font kerning.
    /// </summary>
    public class TMP_KernSafeText: TextMeshProUGUI
    {
        protected override Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled,
            TextWrappingModes textWrapMode)
        {
            if(!enableKerning)
                return base.CalculatePreferredValues(ref fontSize, marginSize, isTextAutoSizingEnabled, textWrapMode);
            enableKerning = false;
            var withoutKerning = base.CalculatePreferredValues(ref fontSize, marginSize, isTextAutoSizingEnabled, textWrapMode);
            
            enableKerning = true;
            var withKerning = base.CalculatePreferredValues(ref fontSize, marginSize, isTextAutoSizingEnabled, textWrapMode);
            
            var kernDifference = Mathf.Abs(withKerning.x - withoutKerning.x);
            withKerning.x += kernDifference;

            return withKerning;
        }
    }
}