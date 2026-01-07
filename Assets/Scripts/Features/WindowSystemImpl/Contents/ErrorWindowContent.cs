using Features.WindowSystemImpl.Data;
using ICVR.Window.Abstract;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.WindowSystemImpl.Contents
{
    public class ErrorWindowContent: AbstractContentWithData<ErrorWindowContentData>
    {
        [SerializeField] private TMP_Text _errorTMP;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _buttonTmp;
        
        public override string Title => Data.Fatal ? "Fatal error": "Error";

        private void Awake()
        {
            _errorTMP.text = Data.ErrorMessage;
            _buttonTmp.text = Data.Fatal ? "Exit" : "Ok";
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        private void OnButtonClick()
        {
            if(Data.Fatal)
                Application.Quit();
            else
                Data.OnClose?.Invoke();
            Close();
        }
    }
}