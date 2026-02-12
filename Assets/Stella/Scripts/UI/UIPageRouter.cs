using System;
using System.Collections.Generic;
using UnityEngine;

namespace StellaGuild.UI
{
    public class UIPageRouter : MonoBehaviour
    {
        [Serializable]
        private class PageEntry
        {
            public UIPageType pageType;
            public UIPage page;
        }

        [SerializeField] private UIPageType initialPage = UIPageType.Title;
        [SerializeField] private List<PageEntry> pageEntries = new();

        private readonly Dictionary<UIPageType, UIPage> _pages = new();
        private UIPage _currentPage;
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        public void ShowInitialPage()
        {
            Initialize();
            Navigate(initialPage);
        }

        public void Navigate(UIPageType pageType)
        {
            Initialize();

            if (!_pages.TryGetValue(pageType, out var nextPage) || nextPage == null)
            {
                Debug.LogError($"Page '{pageType}' is not registered.", this);
                return;
            }

            if (_currentPage == nextPage)
            {
                return;
            }

            if (_currentPage != null)
            {
                _currentPage.Hide();
            }

            _currentPage = nextPage;
            _currentPage.Show();
        }

        public bool Contains(UIPageType pageType)
        {
            Initialize();
            return _pages.ContainsKey(pageType);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _pages.Clear();

            foreach (var entry in pageEntries)
            {
                if (entry == null || entry.page == null)
                {
                    continue;
                }

                if (_pages.ContainsKey(entry.pageType))
                {
                    Debug.LogWarning($"Duplicate page '{entry.pageType}' was ignored.", this);
                    continue;
                }

                _pages.Add(entry.pageType, entry.page);
                entry.page.Hide();
            }
        }
    }
}
