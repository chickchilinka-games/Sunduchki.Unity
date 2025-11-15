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
    public class ButtonView : CheatActionView
    {
        public override string Type => nameof(CheatViewType.Button);

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _buttonText;

        public override void Initialize(int order)
        {
            _buttonText.text = Cheat.Name;
            _button.onClick.AddListener(() => Cheat.Execute());
            base.Initialize(order);
        }
    }
}