using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Basics;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.AuthenticationSystemImpl.Views
{
    public class EmailPasswordInputContent : AbstractContentWithData<EmailPasswordInputContent.InputData>
    {
        public struct InputData : IWindowData
        {
            public UniTaskCompletionSource<(string email, string password)> OnComplete;
        }

        public override string Title => "Authenticate";

        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _cancellationButton;
        [SerializeField] private TMP_InputField _emailInputField;
        [SerializeField] private TMP_InputField _passwordInputField;

        private void Awake()
        {
            _completeButton.onClick.AsObservable()
                .Subscribe(_ => { Data.OnComplete?.TrySetResult((_emailInputField.text, _passwordInputField.text)); })
                .AddTo(this);

            _cancellationButton.onClick.AsObservable()
                .Subscribe(_ => Data.OnComplete?.TrySetCanceled())
                .AddTo(this);
        }
    }
}
