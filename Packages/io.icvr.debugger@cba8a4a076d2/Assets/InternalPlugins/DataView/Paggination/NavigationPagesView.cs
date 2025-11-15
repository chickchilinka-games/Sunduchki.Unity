using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.InternalPlugins.DataView.Paggination
{
    public class NavigationPagesView : MonoBehaviour
    {
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        public Action PrevPageEvent;
        public Action NextPageEvent;

        private void Awake()
        {
            _prevButton.OnClickAsObservable().Subscribe(_ =>
            {
                PrevPageEvent?.Invoke();
            }).AddTo(this);

            _nextButton.OnClickAsObservable().Subscribe(_ =>
            {
                NextPageEvent?.Invoke();
            }).AddTo(this);
        }

        public void ShowNextButton()
        {
            _nextButton.gameObject.SetActive(true);
        }

        public void ShowPreviousButton()
        {
            _prevButton.gameObject.SetActive(true);
        }

        public void HideAll()
        {
            _nextButton.gameObject.SetActive(false);
            _prevButton.gameObject.SetActive(false);
        }
    }
}