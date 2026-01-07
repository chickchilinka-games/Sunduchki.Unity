using System;
using System.Collections.Generic;
using System.Linq;
using Modules.AssetSystem.Editor.AssetPicker.Data;
using Modules.AssetSystem.Editor.AssetPicker.Storage;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.AssetSystem.Editor.AssetPicker.View
{
    internal sealed class AssetRefPickerWindow : EditorWindow
    {
        private const float WindowMaxWidth = 720f;
        private const float WindowMinWidth = 360f;
        private const float WindowHeight = 480f;
        private const float WindowVerticalOffset = 2f;
        private const float BorderWidth = 1f;
        
        private const float HeaderHeight = 20f;
        private const float RowHeight = 33f;
        private const float IconSize = 28f;
        private const float IconMargin = 2f;
        private const float IconSpacing = 36f;
        private const float SearchFieldWidth = 200f;
        
        private const float NameColumnRatio = 0.4f;
        private const float PathColumnRatio = 0.6f;
        private const float PathColumnOffset = 40f;
        private const float PathColumnMargin = 42f;
        
        private static readonly Color HeaderBackgroundColor = new(0.18f, 0.18f, 0.18f);
        private static readonly Color AlternateRowColor = new(0.22f, 0.22f, 0.22f);
        private static readonly Color BorderColor = new(0.12f, 0.12f, 0.12f);
        
        private const string UpButtonText = "⬆ Up";
        private const string ToolbarSearchStyle = "ToolbarSearchTextField";
        private const string FolderIconName = "Folder Icon";
        private const string PrefabIconName = "Prefab Icon";
        
        private Type _type;
        private Action<Object, string> _onPick;
        private List<AssetRefEntry> _entries;
        private string _currentFolder = "";
        private string _search = "";
        private Vector2 _scroll;

        public static void Show(Type type, Action<Object, string> onPick, Rect activatorRect)
        {
            var wnd = CreateInstance<AssetRefPickerWindow>();
            wnd._type = type;
            wnd._onPick = onPick;
            wnd.titleContent = new GUIContent($"Select {type.Name}");
            
            var windowWidth = Mathf.Clamp(activatorRect.width, WindowMinWidth, WindowMaxWidth);
            wnd.position = new Rect(activatorRect.x, activatorRect.yMax + WindowVerticalOffset, windowWidth, WindowHeight);
            wnd.Init();
            wnd.ShowAsDropDown(wnd.position, wnd.position.size);
        }

        private void Init()
        {
            _entries = AssetRefRegistry.Query(_type);
        }

        private IEnumerable<string> GetSubfolders(string folder)
        {
            var prefix = string.IsNullOrEmpty(folder) ? "" : folder + "/";

            string Selector(string dir)
            {
                var rest = dir.Length > prefix.Length ? dir[prefix.Length..] : "";
                var slash = rest.IndexOf('/');
                return slash >= 0 ? rest[..slash] : rest;
            }

            foreach(var item in _entries
                        .Select(e => e.Folder)
                        .Where(f => f.StartsWith(prefix))
                        .Select(Selector)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()
                        .OrderBy(s => s))
            {
                yield return item;
            }
        }
        
        
        private void OnGUI()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (!string.IsNullOrEmpty(_currentFolder) && GUILayout.Button(UpButtonText, EditorStyles.toolbarButton))
                {
                    var slash = _currentFolder.LastIndexOf('/');
                    _currentFolder = slash >= 0 ? _currentFolder.Substring(0, slash) : "";
                }

                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle(ToolbarSearchStyle),
                    GUILayout.Width(SearchFieldWidth));
            }

            EditorGUILayout.Space();

            var headerRect = EditorGUILayout.GetControlRect(false, HeaderHeight);
            EditorGUI.DrawRect(headerRect, HeaderBackgroundColor);
            GUI.Label(new Rect(headerRect.x + IconSpacing, headerRect.y, headerRect.width * NameColumnRatio, headerRect.height),
                "Name", EditorStyles.boldLabel);
            GUI.Label(new Rect(headerRect.x + headerRect.width * NameColumnRatio + PathColumnOffset, headerRect.y,
                headerRect.width * PathColumnRatio - PathColumnMargin, headerRect.height), "Path", EditorStyles.boldLabel);

            DrawBorder();
            
            int rowIndex = 0;
            foreach (var folder in GetSubfolders(_currentFolder))
            {
                DrawRow(folder, $"[{folder}]", isFolder: true, rowIndex: rowIndex++);
            }

            var entries = string.IsNullOrEmpty(_search)
                ? GetEntriesInFolder(_currentFolder)
                : SortEntries(_entries);

            foreach (var e in entries)
            {
                DrawRow(e.Name, e.Folder + "/" + e.Name, isFolder: false, entry: e, rowIndex: rowIndex++);
            }
        }

        private IEnumerable<AssetRefEntry> SortEntries(List<AssetRefEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.AssetRef.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                    yield return entry;
            }
        }
        
        private IEnumerable<AssetRefEntry> GetEntriesInFolder(string folder)
        {
            foreach (var entry in _entries
                         .Where(e => e.Folder == (folder ?? ""))
                         .OrderBy(e => e.Name))
                yield return entry;
        }


        private void DrawRow(string assetName, string path, bool isFolder, AssetRefEntry entry = null,
            int rowIndex = 0)
        {
            var rect = EditorGUILayout.GetControlRect(false, RowHeight);

            if (rowIndex % 2 == 0)
            {
                EditorGUI.DrawRect(rect, AlternateRowColor);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (isFolder)
                {
                    _currentFolder = string.IsNullOrEmpty(_currentFolder)
                        ? assetName
                        : _currentFolder + "/" + assetName;
                }
                else if (entry != null)
                {
                    _onPick?.Invoke(entry.AssetObject, entry.AssetRef);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            Texture preview = null;
            if (!isFolder && entry?.AssetObject != null)
            {
                preview = AssetPreview.GetAssetPreview(entry.AssetObject);
                if (preview == null)
                    preview = AssetPreview.GetMiniThumbnail(entry.AssetObject);
                else
                    Repaint();
            }

            var iconRect = new Rect(rect.x + IconMargin, rect.y + IconMargin, IconSize, IconSize);
            var textRect = new Rect(rect.x + IconSpacing, rect.y + 7, rect.width * NameColumnRatio, 18);
            var pathRect = new Rect(rect.x + rect.width * NameColumnRatio + PathColumnOffset, rect.y + 7, rect.width * PathColumnRatio - PathColumnMargin, 18);

            var content = new GUIContent(preview ?? (isFolder
                ? EditorGUIUtility.IconContent(FolderIconName).image
                : EditorGUIUtility.IconContent(PrefabIconName).image));

            GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit);

            GUI.Label(textRect, assetName, EditorStyles.label);
            GUI.Label(pathRect, path, EditorStyles.miniLabel);
        }

        private void DrawBorder()
        {
            var windowRect = new Rect(0, 0, position.width, position.height);
            
            // Top border
            EditorGUI.DrawRect(new Rect(0, 0, windowRect.width, BorderWidth), BorderColor);
            // Bottom border
            EditorGUI.DrawRect(new Rect(0, windowRect.height - BorderWidth, windowRect.width, BorderWidth), BorderColor);
            // Left border
            EditorGUI.DrawRect(new Rect(0, 0, BorderWidth, windowRect.height), BorderColor);
            // Right border
            EditorGUI.DrawRect(new Rect(windowRect.width - BorderWidth, 0, BorderWidth, windowRect.height), BorderColor);
        }
    }
}