using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Presenters;
using Features.PlayerHandSystemImpl.ViewModel;
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

        private PlayerHandPresenter _presenter;
        private StandardCardViewPool _standardPool;
        private BonusCardViewPool _bonusPool;

        private PlayerHandPresenterState _state;
        private IDisposable _handChangedSubscription;
        private IDisposable _runtimeWatcher;

        private readonly List<RowContainer> _rows = new();
        private readonly Dictionary<StandardCardViewModel, StandardCardView> _standardViews = new();
        private readonly Dictionary<BonusCardViewModel, BonusCardView> _bonusViews = new();

        private class RowContainer
        {
            public RectTransform Root;
            public HorizontalLayoutGroup Layout;
            public int Count;
        }

        [Inject]
        public void Construct(
            PlayerHandPresenter presenter,
            StandardCardViewPool standardPool,
            BonusCardViewPool bonusPool)
        {
            _presenter = presenter;
            _standardPool = standardPool;
            _bonusPool = bonusPool;
        }

        private void OnEnable()
        {
            if (_rowsRoot == null)
            {
                _rowsRoot = GetComponent<RectTransform>();
            }

            _runtimeWatcher = _presenter.State.Subscribe(OnStateChanged);
            OnStateChanged(_presenter.CurrentState);
        }

        private void OnDisable()
        {
            _runtimeWatcher?.Dispose();
            _runtimeWatcher = null;

            OnStateChanged(null);
        }

        private void OnStateChanged(PlayerHandPresenterState state)
        {
            if (ReferenceEquals(_state, state))
            {
                return;
            }

            _handChangedSubscription?.Dispose();
            _handChangedSubscription = null;

            _state = state;
            RenderLayout();

            if (_state != null)
            {
                _handChangedSubscription = _state.HandChanged.Subscribe(_ => RenderLayout());
            }
        }

        private void RenderLayout()
        {
            ResetRows();

            if (_state == null)
            {
                ReleaseAllViews();
                return;
            }

            var slotIndex = 0;
            var activeStandard = new HashSet<StandardCardViewModel>();
            var activeBonus = new HashSet<BonusCardViewModel>();

            foreach (var viewModel in _state.StandardCards)
            {
                activeStandard.Add(viewModel);
                var parent = GetRowTransform(slotIndex++);
                if (!_standardViews.TryGetValue(viewModel, out var view))
                {
                    view = _standardPool.Spawn(parent, viewModel);
                    _standardViews[viewModel] = view;
                }
                else
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                }
            }

            foreach (var viewModel in _state.BonusCards)
            {
                activeBonus.Add(viewModel);
                var parent = GetRowTransform(slotIndex++);
                if (!_bonusViews.TryGetValue(viewModel, out var view))
                {
                    view = _bonusPool.Spawn(parent, viewModel);
                    _bonusViews[viewModel] = view;
                }
                else
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                }
            }

            RemoveMissingStandardViews(activeStandard);
            RemoveMissingBonusViews(activeBonus);
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
            layout.childControlWidth = true;
            layout.childControlHeight = true;
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
            foreach (var view in _standardViews.Values)
            {
                _standardPool.Despawn(view);
            }
            _standardViews.Clear();

            foreach (var view in _bonusViews.Values)
            {
                _bonusPool.Despawn(view);
            }
            _bonusViews.Clear();
        }

        private void RemoveMissingStandardViews(IReadOnlyCollection<StandardCardViewModel> active)
        {
            var toRemove = new List<StandardCardViewModel>();
            foreach (var entry in _standardViews)
            {
                if (!active.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (var viewModel in toRemove)
            {
                if (_standardViews.TryGetValue(viewModel, out var view))
                {
                    _standardViews.Remove(viewModel);
                    AnimateAndDespawnStandard(view).Forget();
                }
            }
        }

        private async UniTaskVoid AnimateAndDespawnStandard(StandardCardView view)
        {
            if (view == null)
            {
                return;
            }

            if (view.HasPendingSetComplete)
            {
                await view.PlaySetCompleteAnimationAsync();
            }
            else
            {
                await view.PlayTransferRemovalAnimationAsync();
            }

            _standardPool.Despawn(view);
        }

        private void RemoveMissingBonusViews(IReadOnlyCollection<BonusCardViewModel> active)
        {
            var toRemove = new List<BonusCardViewModel>();
            foreach (var entry in _bonusViews)
            {
                if (!active.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (var viewModel in toRemove)
            {
                if (_bonusViews.TryGetValue(viewModel, out var view))
                {
                    _bonusViews.Remove(viewModel);
                    _bonusPool.Despawn(view);
                }
            }
        }
    }
}
