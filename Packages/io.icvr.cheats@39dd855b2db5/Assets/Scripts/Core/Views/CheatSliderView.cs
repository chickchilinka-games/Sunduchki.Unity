// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using Cheats.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cheats.Core.Views
{
    public class CheatSliderView : CheatViewWithClampedParameters<float>
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _text;

        public override string Type => nameof(CheatViewType.Slider);
        
        public override void Initialize(int order)
        {
            Debug.Assert(Cheat.MinValue < Cheat.MaxValue, "Cheat min value must be less than max value");
            _text.text = Cheat.Name;
            _slider.value = Cheat.Value;
            _slider.minValue = Cheat.MinValue;
            _slider.maxValue = Cheat.MaxValue;
            _slider.onValueChanged.AddListener(x => Cheat.SetValue(x));
            base.Initialize(order);
        }
    }
}