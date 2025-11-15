using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Storages;
using ObservableCollections;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace DebuggerPlugins.Logger.View
{
    [Serializable]
    internal class MessageFilter : IDisposable
    {
        [SerializeField] private TMP_Dropdown _logTypeSelector;
        [SerializeField] private TMP_Dropdown _categorySelector;
        [SerializeField] private TMP_InputField _searchField;
        [SerializeField] private Toggle _collapseToggle;

        public Observable<Unit> Redraw => _redraw;
        private readonly Subject<Unit> _redraw = new();

        private CompositeDisposable _disposables;

        public bool IsEnabled => _filters.Values.Any(filter => filter.Enabled);

        public ReadOnlyReactiveProperty<bool> IsCollapsed { get; private set; }

        private Dictionary<FilterType, Filter> _filters;

        private class Filter
        {
            public bool Enabled;
            public Func<LoggerMessage, bool> Check;
        }

        private enum FilterType
        {
            LogType,
            Category,
            Search
        }
        
        public void Init(LoggerCategoriesStorage categoriesStorage)
        {
            _disposables = new CompositeDisposable();

            _filters = Enum.GetValues(typeof(FilterType)).OfType<FilterType>()
                .ToDictionary(type => type, _ => new Filter());

            InitFilters(categoriesStorage);
            SubscribeFilters();
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public bool Check(LoggerMessage message)
        {
            return _filters.Values.All(filter => !filter.Enabled || filter.Check(message));
        }

        private void InitFilters(LoggerCategoriesStorage categoriesStorage)
        {
            _logTypeSelector.ClearOptions();
            _logTypeSelector.AddOptions(new List<string>
            {
                "Any",
                nameof(LogType.Log), nameof(LogType.Warning),
                nameof(LogType.Assert), nameof(LogType.Error), nameof(LogType.Exception)
            });

            _categorySelector.ClearOptions();
            _categorySelector.options.Add(new TMP_Dropdown.OptionData("Any"));

            foreach (var item in categoriesStorage.Items)
            {
                _categorySelector.options.Add(new TMP_Dropdown.OptionData(item));
            }
            
            _categorySelector.RefreshShownValue();
            
            categoriesStorage.Items
                .ObserveAdd()
                .Subscribe(args =>
                {
                    _categorySelector.options.Add(new TMP_Dropdown.OptionData(args.Value));
                    _categorySelector.RefreshShownValue();
                })
                .AddTo(_disposables);
        }

        private void SubscribeFilters()
        {
            IsCollapsed = _collapseToggle.onValueChanged.AsObservable().ToReadOnlyReactiveProperty();
            IsCollapsed
                .Subscribe(_ =>
                {
                    _redraw.OnNext(new Unit());
                })
                .AddTo(_disposables);

            _logTypeSelector.onValueChanged.AsObservable()
                .Subscribe(index =>
                {
                    var enabled = index != 0;
                    Enum.TryParse<LogType>(_logTypeSelector.options[index].text, out var logType);
                    Func<LoggerMessage, bool> check = null;
                    
                    if (enabled)
                    {
                        check = message => message.LogType == logType;
                    }
                    
                    ApplyFilter(FilterType.LogType, enabled, check);
                })
                .AddTo(_disposables);

            _categorySelector.onValueChanged.AsObservable()
                .Subscribe(index =>
                {
                    var enabled = index != 0;
                    Func<LoggerMessage, bool> check = null;
                    
                    if (enabled)
                    {
                        check = message => message.Category.Equals(_categorySelector.options[index].text);
                    }
                    
                    ApplyFilter(FilterType.Category, enabled, check);
                })
                .AddTo(_disposables);

            _searchField.onValueChanged.AsObservable()
                .Debounce(TimeSpan.FromSeconds(0.5f))
                .Subscribe(input =>
                {
                    var enabled = !string.IsNullOrEmpty(input);
                    Func<LoggerMessage, bool> check = null;
                    
                    if (enabled)
                    {
                        check = message =>
                        {
                            try
                            {
                                return Regex.IsMatch(message.Message.ToLower(), input.ToLower());
                            }
                            catch (ArgumentException)
                            {
                                return false;
                            }
                        };
                        
                    }
                    
                    ApplyFilter(FilterType.Search, enabled, check);
                })
                .AddTo(_disposables);
        }

        private void ApplyFilter(FilterType type, bool enabled, Func<LoggerMessage, bool> check)
        {
            _filters[type].Enabled = enabled;
            _filters[type].Check = check;
            _redraw.OnNext(new Unit());
        }
    }
}