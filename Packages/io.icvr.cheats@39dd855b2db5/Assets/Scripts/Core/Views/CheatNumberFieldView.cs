using System;
using Cheats.Core.Data;
using TMPro;
using R3;
using UnityEngine;

namespace Cheats.Core.Views
{
    public class CheatNumberFieldView : CheatViewWithClampedParameters<float>
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_InputField _inputField;
        
        private readonly CompositeDisposable _disposable = new();

        public override string Type => nameof(CheatViewType.NumberField);
        
        public override void Initialize(int order)
        {
            _text.text = Cheat.Name;
            _inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            SetInputField(Cheat.Value);
            Cheat.ValueChanged
                .Subscribe(SetInputField)
                .AddTo(_disposable);
            _inputField.onEndEdit.AddListener(input =>
            {
                if (float.TryParse(input, out var value))
                {
                    value = Mathf.Clamp(value, Cheat.MinValue, Cheat.MaxValue);
                    Cheat.SetValue(value);
                }
                else
                {
                    SetInputField(Cheat.Value);
                }
            });
            base.Initialize(order);
        }

        private void OnDestroy()
        {
            _disposable.Dispose();
        }

        private void SetInputField(float value)
        {
            _inputField.text = value.ToString("0.##");
        }
    }
}