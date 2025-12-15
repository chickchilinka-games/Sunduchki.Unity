using System;
using System.Collections.Generic;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Rules;
using Features.PlayerHandSystemImpl.Service;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public class PlayerHandHolder : MonoBehaviour
    {
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField, Min(1)] private int _maxItemsPerRow = 5;
        [SerializeField] private float _rowItemSpacing = 12f;
        [SerializeField] private TextAnchor _rowAlignment = TextAnchor.MiddleCenter;

        private PlayerHandViewRule _viewRule;
        private StandardCardViewPool _standardPool;
        private BonusCardViewPool _bonusPool;

        private PlayerHandViewRuntime _runtime;
        private IDisposable _handChangedSubscription;
        private IDisposable _runtimeWatcher;

        private readonly List<RowContainer> _rows = new();
        private readonly List<StandardCardView> _activeStandardViews = new();
        private readonly List<BonusCardView> _activeBonusViews = new();

        private class RowContainer
        {
            public RectTransform Root;
            public HorizontalLayoutGroup Layout;
            public int Count;
        }

        [Inject]
        public void Construct(
            PlayerHandViewRule viewRule,
            StandardCardViewPool standardPool,
            BonusCardViewPool bonusPool)
        {
            _viewRule = viewRule;
            _standardPool = standardPool;
            _bonusPool = bonusPool;
        }

        private void OnEnable()
        {
            if (_rowsRoot == null)
            {
                _rowsRoot = GetComponent<RectTransform>();
            }

            _runtimeWatcher = _viewRule.Runtime.Subscribe(OnRuntimeChanged);
            OnRuntimeChanged(_viewRule.CurrentRuntime);
        }

        private void OnDisable()
        {
            _runtimeWatcher?.Dispose();
            _runtimeWatcher = null;

            OnRuntimeChanged(null);
        }

        private void OnRuntimeChanged(PlayerHandViewRuntime runtime)
        {
            if (ReferenceEquals(_runtime, runtime))
            {
                return;
            }

            _handChangedSubscription?.Dispose();
            _handChangedSubscription = null;

            _runtime = runtime;
            RenderLayout();

            if (_runtime != null)
            {
                _handChangedSubscription = _runtime.HandChanged.Subscribe(_ => RenderLayout());
            }
        }

        private void RenderLayout()
        {
            ReleaseAllViews();
            ResetRows();

            if (_runtime == null)
            {
                return;
            }

            var slotIndex = 0;

            foreach (var viewModel in _runtime.StandardCards)
            {
                var parent = GetRowTransform(slotIndex++);
                var view = _standardPool.Spawn(parent, viewModel);
                _activeStandardViews.Add(view);
            }

            foreach (var viewModel in _runtime.BonusCards)
            {
                var parent = GetRowTransform(slotIndex++);
                var view = _bonusPool.Spawn(parent, viewModel);
                _activeBonusViews.Add(view);
            }
        }

        private void ResetRows()
        {
            foreach (var row in _rows)
            {
                row.Count = 0;
                row.Root.gameObject.SetActive(false);
            }
        }

        private RectTransform GetRowTransform(int slotIndex)
        {
            var itemsPerRow = Mathf.Max(1, _maxItemsPerRow);
            var rowIndex = slotIndex / itemsPerRow;
            EnsureRow(rowIndex);
            var row = _rows[rowIndex];
            row.Count++;
            row.Root.gameObject.SetActive(true);
            return row.Root;
        }

        private void EnsureRow(int rowIndex)
        {
            while (_rows.Count <= rowIndex)
            {
                _rows.Add(CreateRow(_rows.Count));
            }
        }

        private RowContainer CreateRow(int index)
        {
            var go = new GameObject($"Row_{index}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_rowsRoot, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = _rowItemSpacing;
            layout.childAlignment = _rowAlignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return new RowContainer
            {
                Root = rect,
                Layout = layout,
                Count = 0
            };
        }

        private void ReleaseAllViews()
        {
            foreach (var view in _activeStandardViews)
            {
                _standardPool.Despawn(view);
            }
            _activeStandardViews.Clear();

            foreach (var view in _activeBonusViews)
            {
                _bonusPool.Despawn(view);
            }
            _activeBonusViews.Clear();
        }
    }
}
