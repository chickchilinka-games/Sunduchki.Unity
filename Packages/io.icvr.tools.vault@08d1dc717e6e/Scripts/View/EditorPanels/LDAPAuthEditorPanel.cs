using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Entities;
using ICVR.Tools.Vault.Interfaces;
using UnityEditor;
using UnityEngine;

namespace ICVR.Tools.Vault.View
{
    internal class LDAPAuthEditorPanel : IAuthEditorPanel
    {
        private const string NicknameKey = "VaultNickname";
        private const string PasswordKey = "VaultPassword";
        private readonly Vector2 _inputFieldPercents = new(0.7f, 0.25f);
        private readonly Vector2 _inputLabelPercents = new(0.25f, 0.25f);

        private readonly Vector2 _titlePercents = new(1f, 0.15f);

        private string _nickname = string.Empty;
        private string _password = string.Empty;

        public AuthType AuthType => AuthType.LDAP;

        public void Initialize()
        {
            _nickname = EditorPrefs.GetString(NicknameKey, string.Empty);
            _password = EditorPrefs.GetString(PasswordKey, string.Empty);
        }

        public void Draw(Vector2 areaSize)
        {
            GUILayout.BeginVertical(GUILayout.Width(areaSize.x), GUILayout.Height(areaSize.y));
            {
                GUILayout.Space(0.05f * areaSize.y);

                DrawTitle(areaSize);

                GUILayout.Space(0.1f * areaSize.y);

                DrawNicknameField(areaSize);

                GUILayout.Space(0.05f * areaSize.y);

                DrawPasswordField(areaSize);

                GUILayout.Space(0.05f * areaSize.y);
            }
            GUILayout.EndVertical();
        }

        public string GetCredentials()
        {
            EditorPrefs.SetString(NicknameKey, _nickname);
            EditorPrefs.SetString(PasswordKey, _password);

            return new LDAPCredentialsBuilder(_nickname, _password).Build();
        }

        private void DrawTitle(Vector2 areaSize)
        {
            var labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("Please, enter your credentials below:",
                labelStyle,
                GUILayout.Width(_titlePercents.x * areaSize.x),
                GUILayout.Height(_titlePercents.y * areaSize.y));
        }

        private void DrawNicknameField(Vector2 areaSize)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(_inputFieldPercents.y * areaSize.y));
            {
                var textFieldHeight = _inputFieldPercents.y * areaSize.y;
                var textFieldStyle = new GUIStyle(GUI.skin.textField);
                textFieldStyle.fontSize = (int)(textFieldHeight / 20f * 15);

                GUILayout.Label("Nickname:",
                    GUILayout.Width(_inputLabelPercents.x * areaSize.x),
                    GUILayout.Height(_inputLabelPercents.y * areaSize.y));

                _nickname = GUILayout.TextField(_nickname,
                    20,
                    textFieldStyle,
                    GUILayout.Width(_inputFieldPercents.x * areaSize.x),
                    GUILayout.Height(textFieldHeight));
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPasswordField(Vector2 areaSize)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(_inputFieldPercents.y * areaSize.y));
            {
                var textFieldHeight = _inputFieldPercents.y * areaSize.y;
                var textFieldStyle = new GUIStyle(GUI.skin.textField);
                textFieldStyle.fontSize = (int)(textFieldHeight / 20f * 15);

                GUILayout.Label("Password:",
                    GUILayout.Width(_inputLabelPercents.x * areaSize.x),
                    GUILayout.Height(_inputLabelPercents.y * areaSize.y));

                _password = GUILayout.PasswordField(_password,
                    '*',
                    20,
                    textFieldStyle,
                    GUILayout.Width(_inputFieldPercents.x * areaSize.x),
                    GUILayout.Height(textFieldHeight));
            }
            GUILayout.EndHorizontal();
        }
    }
}