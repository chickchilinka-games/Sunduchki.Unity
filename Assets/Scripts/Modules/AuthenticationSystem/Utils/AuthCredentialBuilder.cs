using System;
using System.Collections.Generic;
using System.Linq;
using Modules.AuthenticationSystem.Data;

namespace Modules.AuthenticationSystem.Utils
{
    public sealed class AuthCredentialBuilder
    {
        private readonly AuthType _authType;
        private readonly Dictionary<string, object> _parameters = new();
        private string _username;

        private AuthCredentialBuilder(AuthType authType)
        {
            _authType = authType;
        }

        public static AuthCredentialBuilder For(AuthType authType)
        {
            if (authType == AuthType.None)
            {
                throw new ArgumentException("AuthType.None is not a valid authentication provider.", nameof(authType));
            }

            return new AuthCredentialBuilder(authType);
        }

        public AuthCredentialBuilder WithUsername(string username)
        {
            _username = username;
            return this;
        }

        public AuthCredentialBuilder WithIdToken(string idToken) => WithParameter(ParameterKeys.IdToken, idToken);

        public AuthCredentialBuilder WithAccessToken(string accessToken) => WithParameter(ParameterKeys.AccessToken, accessToken);

        public AuthCredentialBuilder WithRawNonce(string rawNonce) => WithParameter(ParameterKeys.RawNonce, rawNonce);

        public AuthCredentialBuilder WithDisplayName(string displayName) => WithParameter(ParameterKeys.DisplayName, displayName);

        public AuthCredentialBuilder WithScopes(IEnumerable<string> scopes)
        {
            var scopeArray = scopes?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
            return WithParameter(ParameterKeys.Scopes, scopeArray);
        }

        public AuthCredentialBuilder WithCustomParameters(IDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                return WithParameter(ParameterKeys.CustomParameters, null);
            }

            return WithParameter(ParameterKeys.CustomParameters, new Dictionary<string, string>(parameters));
        }

        public AuthCredentialBuilder WithParameter(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Parameter key cannot be null or whitespace.", nameof(key));
            }

            if (value == null)
            {
                _parameters.Remove(key);
            }
            else
            {
                _parameters[key] = value;
            }

            return this;
        }

        public AuthCredential Build()
        {
            if (_authType == AuthType.None)
            {
                throw new InvalidOperationException("Invalid AuthType.None used for credential creation.");
            }

            return new AuthCredential(_authType, _username, _parameters);
        }

        public static GoogleAuthCredential ParseGoogle(AuthCredential credential)
        {
            var webCredential = ParseWebInternal(credential, AuthType.Google);

            credential.TryGet(ParameterKeys.IdToken, out string idToken);
            credential.TryGet(ParameterKeys.AccessToken, out string accessToken);

            return new GoogleAuthCredential(
                webCredential.Username,
                webCredential.DisplayName,
                webCredential.Scopes,
                webCredential.CustomParameters,
                idToken,
                accessToken);
        }

        public static AppleAuthCredential ParseApple(AuthCredential credential)
        {
            var webCredential = ParseWebInternal(credential, AuthType.Apple);

            credential.TryGet(ParameterKeys.IdToken, out string idToken);
            credential.TryGet(ParameterKeys.RawNonce, out string rawNonce);
            credential.TryGet(ParameterKeys.AccessToken, out string accessToken);

            return new AppleAuthCredential(
                webCredential.Username,
                webCredential.DisplayName,
                webCredential.Scopes,
                webCredential.CustomParameters,
                idToken,
                rawNonce,
                accessToken);
        }

        public static WebAuthCredential ParseWeb(AuthCredential credential)
        {
            return ParseWebInternal(credential, expectedAuthType: credential?.AuthType ?? AuthType.None);
        }

        private static WebAuthCredential ParseWebInternal(AuthCredential credential, AuthType expectedAuthType)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (expectedAuthType != AuthType.None && credential.AuthType != expectedAuthType)
            {
                throw new ArgumentException($"Credential must be of type {expectedAuthType}, but was {credential.AuthType}.", nameof(credential));
            }

            if (credential.AuthType == AuthType.None)
            {
                throw new ArgumentException("Credential must specify a concrete auth type.", nameof(credential));
            }

            credential.TryGet(ParameterKeys.DisplayName, out string displayName);

            var scopes = ExtractScopes(credential.Parameters);
            var customParameters = ExtractCustomParameters(credential.Parameters);

            return new WebAuthCredential(
                credential.AuthType,
                credential.Username,
                displayName ?? credential.Username,
                scopes,
                customParameters);
        }

        private static IReadOnlyList<string> ExtractScopes(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue(ParameterKeys.Scopes, out var raw) && raw != null)
            {
                switch (raw)
                {
                    case string[] array:
                        return array.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
                    case IEnumerable<string> enumerable:
                        return enumerable.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
                    case string single:
                        return new[] { single.Trim() };
                }
            }

            return Array.Empty<string>();
        }

        private static IReadOnlyDictionary<string, string> ExtractCustomParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue(ParameterKeys.CustomParameters, out var raw) && raw != null)
            {
                switch (raw)
                {
                    case IDictionary<string, string> dictionary:
                        return new Dictionary<string, string>(dictionary);
                    case IEnumerable<KeyValuePair<string, string>> pairs:
                        return pairs.ToDictionary(p => p.Key, p => p.Value);
                }
            }

            return new Dictionary<string, string>();
        }

        public static class ParameterKeys
        {
            public const string Email = "email";
            public const string IdToken = "idToken";
            public const string AccessToken = "accessToken";
            public const string RawNonce = "rawNonce";
            public const string DisplayName = "displayName";
            public const string Scopes = "scopes";
            public const string CustomParameters = "customParameters";
        }
    }
}
