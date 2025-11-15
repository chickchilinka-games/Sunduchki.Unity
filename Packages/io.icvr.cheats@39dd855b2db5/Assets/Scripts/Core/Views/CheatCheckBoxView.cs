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
    public class CheatCheckBoxView  : CheatViewWithParameters<bool>
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _text;

        public override string Type => nameof(CheatViewType.CheckBox);
        
        public override void Initialize(int order)
        {
            _text.text = Cheat.Name;
            _toggle.isOn = Cheat.Value;
            
            _toggle.onValueChanged.AddListener(x => Cheat.SetValue(x));
            base.Initialize(order);
        }
    }
}