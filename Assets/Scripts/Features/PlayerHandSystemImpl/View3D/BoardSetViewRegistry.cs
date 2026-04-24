using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BoardSetViewRegistry
    {
        private readonly Transform _rankSetsRoot;
        private readonly Transform _bonusSetsRoot;
        private readonly RankCardSet3DView _rankSetPrefab;
        private readonly BonusCardSet3DView _bonusSetPrefab;
        private readonly Dictionary<string, RankCardSet3DView> _rankSetViews =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BonusCardSet3DView> _bonusSetViews =
            new(StringComparer.OrdinalIgnoreCase);

        public BoardSetViewRegistry(
            Transform rankSetsRoot,
            Transform bonusSetsRoot,
            RankCardSet3DView rankSetPrefab,
            BonusCardSet3DView bonusSetPrefab)
        {
            _rankSetsRoot = rankSetsRoot;
            _bonusSetsRoot = bonusSetsRoot;
            _rankSetPrefab = rankSetPrefab;
            _bonusSetPrefab = bonusSetPrefab;
        }

        public async UniTask SyncRankSetsAsync(
            IReadOnlyList<BoardRankSetSnapshot> snapshots,
            BoardCardSpriteResolver resolver,
            int setsPerRow,
            float spacingX,
            float spacingZ,
            CancellationToken cancellationToken = default)
        {
            var safeSnapshots = snapshots ?? Array.Empty<BoardRankSetSnapshot>();
            var staleKeys = ResolveStaleRankSetKeys(safeSnapshots);
            foreach (var staleKey in staleKeys)
            {
                RemoveRankSet(staleKey);
            }

            var orderedKeys = new List<string>(safeSnapshots.Count);
            for (var i = 0; i < safeSnapshots.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var snapshot = safeSnapshots[i];
                if (!_rankSetViews.TryGetValue(snapshot.Rank, out var setView) || setView == null)
                {
                    setView = CreateRankSetView(snapshot.Rank);
                    if (setView == null)
                    {
                        continue;
                    }

                    _rankSetViews[snapshot.Rank] = setView;
                }

                orderedKeys.Add(snapshot.Rank);
                await setView.BindAsync(snapshot.Rank, snapshot.Suits, resolver, cancellationToken);
            }

            LayoutRankSets(orderedKeys, setsPerRow, spacingX, spacingZ);
        }

        public async UniTask SyncBonusSetsAsync(
            IReadOnlyList<BoardBonusSetSnapshot> snapshots,
            BoardCardSpriteResolver resolver,
            int setsPerRow,
            float spacingX,
            float spacingZ,
            CancellationToken cancellationToken = default)
        {
            var safeSnapshots = snapshots ?? Array.Empty<BoardBonusSetSnapshot>();
            var staleKeys = ResolveStaleBonusSetKeys(safeSnapshots);
            foreach (var staleKey in staleKeys)
            {
                RemoveBonusSet(staleKey);
            }

            var orderedKeys = new List<string>(safeSnapshots.Count);
            for (var i = 0; i < safeSnapshots.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var snapshot = safeSnapshots[i];
                if (!_bonusSetViews.TryGetValue(snapshot.BonusType, out var setView) || setView == null)
                {
                    setView = CreateBonusSetView(snapshot.BonusType);
                    if (setView == null)
                    {
                        continue;
                    }

                    _bonusSetViews[snapshot.BonusType] = setView;
                }

                orderedKeys.Add(snapshot.BonusType);
                await setView.BindAsync(snapshot.BonusType, snapshot.Count, resolver, cancellationToken);
            }

            LayoutBonusSets(orderedKeys, setsPerRow, spacingX, spacingZ);
        }

        public void Clear()
        {
            foreach (var pair in _rankSetViews)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.ResetView();
                DestroySafe(pair.Value.gameObject);
            }

            _rankSetViews.Clear();

            foreach (var pair in _bonusSetViews)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.ResetView();
                DestroySafe(pair.Value.gameObject);
            }

            _bonusSetViews.Clear();
        }

        private List<string> ResolveStaleRankSetKeys(IReadOnlyList<BoardRankSetSnapshot> snapshots)
        {
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < snapshots.Count; i++)
            {
                active.Add(snapshots[i].Rank);
            }

            var stale = new List<string>();
            foreach (var pair in _rankSetViews)
            {
                if (!active.Contains(pair.Key))
                {
                    stale.Add(pair.Key);
                }
            }

            return stale;
        }

        private List<string> ResolveStaleBonusSetKeys(IReadOnlyList<BoardBonusSetSnapshot> snapshots)
        {
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < snapshots.Count; i++)
            {
                active.Add(snapshots[i].BonusType);
            }

            var stale = new List<string>();
            foreach (var pair in _bonusSetViews)
            {
                if (!active.Contains(pair.Key))
                {
                    stale.Add(pair.Key);
                }
            }

            return stale;
        }

        private RankCardSet3DView CreateRankSetView(string rank)
        {
            if (_rankSetPrefab == null)
            {
                Debug.LogWarning("[PlayerHand3D] Rank set prefab is not assigned.");
                return null;
            }

            var parent = _rankSetsRoot != null ? _rankSetsRoot : _rankSetPrefab.transform.parent;
            var view = UnityEngine.Object.Instantiate(_rankSetPrefab, parent);
            view.name = $"RankSet_{rank}";
            return view;
        }

        private BonusCardSet3DView CreateBonusSetView(string bonusType)
        {
            if (_bonusSetPrefab == null)
            {
                Debug.LogWarning("[PlayerHand3D] Bonus set prefab is not assigned.");
                return null;
            }

            var parent = _bonusSetsRoot != null ? _bonusSetsRoot : _bonusSetPrefab.transform.parent;
            var view = UnityEngine.Object.Instantiate(_bonusSetPrefab, parent);
            view.name = $"BonusSet_{bonusType}";
            return view;
        }

        private void LayoutRankSets(IReadOnlyList<string> orderedKeys, int setsPerRow, float spacingX, float spacingZ)
        {
            var perRow = Mathf.Max(1, setsPerRow);
            for (var i = 0; i < orderedKeys.Count; i++)
            {
                if (!_rankSetViews.TryGetValue(orderedKeys[i], out var setView) || setView == null)
                {
                    continue;
                }

                var col = i % perRow;
                var row = i / perRow;
                setView.transform.localPosition = new Vector3(col * spacingX, 0f, row * spacingZ);
            }
        }

        private void LayoutBonusSets(IReadOnlyList<string> orderedKeys, int setsPerRow, float spacingX, float spacingZ)
        {
            var perRow = Mathf.Max(1, setsPerRow);
            for (var i = 0; i < orderedKeys.Count; i++)
            {
                if (!_bonusSetViews.TryGetValue(orderedKeys[i], out var setView) || setView == null)
                {
                    continue;
                }

                var col = i % perRow;
                var row = i / perRow;
                setView.transform.localPosition = new Vector3(col * spacingX, 0f, row * spacingZ);
            }
        }

        private void RemoveRankSet(string key)
        {
            if (!_rankSetViews.Remove(key, out var view) || view == null)
            {
                return;
            }

            view.ResetView();
            DestroySafe(view.gameObject);
        }

        private void RemoveBonusSet(string key)
        {
            if (!_bonusSetViews.Remove(key, out var view) || view == null)
            {
                return;
            }

            view.ResetView();
            DestroySafe(view.gameObject);
        }

        private static void DestroySafe(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
