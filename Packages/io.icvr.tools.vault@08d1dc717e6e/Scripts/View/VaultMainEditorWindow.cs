using System;
using System.Collections.Generic;
using System.Linq;
using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Entities;
using ICVR.Tools.Vault.Interfaces;
using ICVR.Tools.Vault.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ICVR.Tools.Vault.View
{
    internal class VaultMainEditorWindow : EditorWindow
    {
        private const float _aspectRatio = 2.1f;

        private static readonly List<IAuthEditorPanel> _authPanels;
        private static VaultMainEditorWindow _window;
        private static Vector2 _windowSize;
        private static Action<bool> _callback;
        private static VaultConnectionSettings _connectionSettings;
        private readonly Vector2 _buttonPercents = new(1f, 0.3f);
        private int _environmentSelectedIndex = 0;
        private bool _isConnectionProcess;

        private IAuthEditorPanel _selectedAuthProcess;

        static VaultMainEditorWindow()
        {
            _authPanels = FindAuthPanels();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            {
                if (!Connection.Instance.Status.Equals(VaultStatus.Connected))
                {
                    if (_selectedAuthProcess == null)
                    {
                        DrawAuthorizationButtons(_windowSize);
                    }
                    else
                    {
                        var navigationHeight = _windowSize.y * _buttonPercents.y;
                        var panelHeight = _windowSize.y - navigationHeight;

                        DrawAuthorizationPanel(new Vector2(_windowSize.x, panelHeight));
                        DrawNavigationPanel(new Vector2(_windowSize.x, navigationHeight));
                    }
                }
                else
                {
                    GUILayout.Label("You have logged in already!");
                }
            }
            GUILayout.EndVertical();
        }

        [MenuItem("Custom/Vault/Login")]
        public static void OpenByEditor()
        {
            Open();
        }

        public static void Open(Action<bool> callback = null, VaultConnectionSettings connectionSettings = null)
        {
            _windowSize = new Vector2(4 * Screen.dpi, 1.5f * Screen.dpi);
            _window = (VaultMainEditorWindow)GetWindow(typeof(VaultMainEditorWindow));
            _window.titleContent = new GUIContent("Vault");
            _window.maxSize = _windowSize;
            _window.minSize = _windowSize;
            _callback = callback;
            _connectionSettings = connectionSettings;

            _window.Show();
        }

        public static void Close()
        {
            if (_window == null) return;

            ((EditorWindow)_window).Close();

            if (_callback != null) _callback.Invoke(false);
        }

        private void DrawAuthorizationPanel(Vector2 areaSize)
        {
            _selectedAuthProcess.Draw(areaSize);
        }

        private void DrawNavigationPanel(Vector2 areaSize)
        {
            var horizontalOffset = areaSize.x * 0.3f;
            var verticalOffset = areaSize.y * 0.2f;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset((int)horizontalOffset,
                    (int)horizontalOffset,
                    (int)verticalOffset,
                    (int)verticalOffset)
            };


            if (GUILayout.Button(!_isConnectionProcess ? "Log in" : "Cancel",
                    buttonStyle,
                    GUILayout.Width(areaSize.x - horizontalOffset * 2),
                    GUILayout.Height(areaSize.y - verticalOffset * 2)))
            {
                if (!_isConnectionProcess)
                    OnClickLoginButton();
                else
                    OnClickCancelButton();
            }
        }

        private void OnClickLoginButton()
        {
            var credentials = _selectedAuthProcess.GetCredentials();

            _isConnectionProcess = true;

            var success = Connection.Instance.Initialize(credentials, _connectionSettings);

            _isConnectionProcess = false;

            if (success)
            {
                _callback?.Invoke(true);
                _callback = null;
                _connectionSettings = null;

                ((EditorWindow)_window).Close();
            }
        }

        private void OnClickCancelButton()
        {
            _isConnectionProcess = false;

            Connection.Instance.Dispose();
        }

        private void DrawAuthorizationButtons(Vector2 areaSize)
        {
            var labelStyle = GUI.skin.label;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            if (_authPanels == null || !_authPanels.Any())
            {
                GUILayout.Label("No auth cases...");

                return;
            }

            var titleHeight = areaSize.y * 0.15f;

            GUILayout.Space(10);

            GUILayout.Label("Select auth type:", GUILayout.Width(areaSize.x), GUILayout.Height(titleHeight));

            GUILayout.Space(10);

            var offset = areaSize.x * 0.05f;
            var authPanelsCount = _authPanels.Count;
            var buttonWidth = (areaSize.x - (authPanelsCount + 1) * offset) / authPanelsCount;
            var buttonHeight = buttonWidth / _aspectRatio;
            var topOffset = (areaSize.y - titleHeight - buttonHeight) / 2;
            var layoutStyle = GUI.skin.box;
            layoutStyle.margin = new RectOffset((int)offset, (int)offset, (int)topOffset, (int)topOffset);

            GUILayout.BeginHorizontal();
            {
                foreach (var authPanel in _authPanels)
                {
                    GUILayout.Space(offset);

                    DrawButton(authPanel.AuthType.ToString(),
                        new Vector2(buttonWidth, buttonHeight),
                        () => SetCurrentAuthType(authPanel.AuthType));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawButton(string label, Vector2 size, UnityAction onClick)
        {
            if (GUILayout.Button(label, GUILayout.Width(size.x), GUILayout.Height(size.y))) onClick?.Invoke();
        }

        private void SetCurrentAuthType(AuthType authType)
        {
            _selectedAuthProcess = _authPanels.FirstOrDefault(item => item.AuthType.Equals(authType));
        }

        private static List<IAuthEditorPanel> FindAuthPanels()
        {
            var authPanels = new List<IAuthEditorPanel>();
            var implementations = ReflectionUtils.GetImplementationsFromCurrentAssembly<IAuthEditorPanel>();

            foreach (var type in implementations)
            {
                var authPanel = ReflectionUtils.CreateInstance<IAuthEditorPanel>(type);

                authPanels.Add(authPanel);
            }

            return authPanels;
        }
    }
}