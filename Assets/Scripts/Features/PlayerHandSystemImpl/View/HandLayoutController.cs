using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.PlayerHandSystemImpl.View
{
    internal sealed class HandLayoutController
    {
        private readonly int _maxItemsPerRow;
        private readonly float _rowItemSpacing;
        private readonly TextAnchor _rowAlignment;
        private readonly List<HandLayoutRowContainer> _rows = new();
        private RectTransform _rowsRoot;

        public HandLayoutController(int maxItemsPerRow, float rowItemSpacing, TextAnchor rowAlignment)
        {
            _maxItemsPerRow = Mathf.Max(1, maxItemsPerRow);
            _rowItemSpacing = rowItemSpacing;
            _rowAlignment = rowAlignment;
        }

        public void SetRoot(RectTransform rowsRoot)
        {
            _rowsRoot = rowsRoot;
        }

        public void ResetRows()
        {
            foreach (var row in _rows)
            {
                row.Count = 0;
                if (row.Root != null)
                {
                    row.Root.gameObject.SetActive(false);
                }
            }
        }

        public RectTransform GetRowTransform(int slotIndex)
        {
            if (_rowsRoot == null)
            {
                return null;
            }

            var rowIndex = Mathf.Max(0, slotIndex) / _maxItemsPerRow;
            EnsureRow(rowIndex);
            var row = _rows[rowIndex];
            row.Count++;
            row.Root.gameObject.SetActive(true);
            return row.Root;
        }

        public void ForceRebuild()
        {
            if (_rowsRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
        }

        private void EnsureRow(int rowIndex)
        {
            while (_rows.Count <= rowIndex)
            {
                _rows.Add(CreateRow(_rows.Count));
            }
        }

        private HandLayoutRowContainer CreateRow(int index)
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

            return new HandLayoutRowContainer
            {
                Root = rect,
                Count = 0
            };
        }
    }
}
