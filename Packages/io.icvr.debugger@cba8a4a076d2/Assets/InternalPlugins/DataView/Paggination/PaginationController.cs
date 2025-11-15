using System.Collections.Generic;
using UnityEngine;

namespace Assets.InternalPlugins.DataView.Paggination
{
    public class PaginationController: MonoBehaviour
    {
        [SerializeField] private GameObject _pagePrefab;
        [SerializeField] List<PageView> _pages;
        [SerializeField] private RectTransform _content;
        [SerializeField] private NavigationPagesView _navigationPagesView;

        private int _currentPage = 0;

        private void Awake()
        {
            _navigationPagesView.NextPageEvent += OnNextPageEvent;
            _navigationPagesView.PrevPageEvent += OnPrevPageEvent;

            UpdatePage();
        }

        private void OnDestroy()
        {
            _navigationPagesView.NextPageEvent -= OnNextPageEvent;
            _navigationPagesView.PrevPageEvent -= OnPrevPageEvent;
        }

        private void OnNextPageEvent()
        {
            _pages[_currentPage].Hide();
            _currentPage++;
            _pages[_currentPage].Show();
            UpdatePage();
        }

        private void OnPrevPageEvent()
        {
            _pages[_currentPage].Hide();
            _currentPage--;
            _pages[_currentPage].Show();
            UpdatePage();
        }

        private void UpdatePage()
        {
            _navigationPagesView.HideAll();

            if (_currentPage < (_pages.Count - 1))
            {
                _navigationPagesView.ShowNextButton();
            }

            if (_currentPage > 0)
            {
                _navigationPagesView.ShowPreviousButton();
            }
        }

        public void AddContent(RectTransform rectTransform)
        {
            _pages ??= new List<PageView>();

            if (_pages.Count == 0)
            {
                AddPage();
            }

            var lastPage = _pages[_pages.Count - 1];
            var availableHeight = lastPage.GetFreeHeight();
            if (rectTransform.sizeDelta.y > availableHeight)
            {
                AddPage();
            }

            lastPage = _pages[_pages.Count - 1];
            lastPage.AddContent(rectTransform);

            UpdatePage();
        }

        private void AddPage()
        {
            var page = Instantiate(_pagePrefab, _content).GetComponent<PageView>();
            page.GetComponent<RectTransform>().localScale = Vector3.one;
            _pages.Add(page);
            var numberPage = _pages.Count - 1;
            if (_currentPage != numberPage)
                page.gameObject.SetActive(false);
        }
    }
}