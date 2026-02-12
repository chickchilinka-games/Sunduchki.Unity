#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Exceptions;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;
using UnityEngine;

namespace Modules.AuthenticationSystem.Providers
{
    public class NativeAppleCredentialProvider: ICredentialProvider
    {
        public AuthType AuthType => AuthType.Apple;
        public async UniTask<AuthCredential> AcquireCredentialAsync()
        {
            var deserializer = new PayloadDeserializer();
            var appleAuthManager = new AppleAuthManager(deserializer);
            
            var rawNonce = Guid.NewGuid().ToString("N");
            var hashedNonce = ComputeSHA256(rawNonce);
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, hashedNonce);

            TaskCompletionSource<IAppleIDCredential> tcs = new();
            appleAuthManager.LoginWithAppleId(loginArgs, credential =>
            {
                tcs.TrySetResult(credential as IAppleIDCredential);
            }, error =>
            {
                if (IsUserCancellation(error))
                {
                    Debug.Log($"[AppleAuth] User cancelled authorization: {error.LocalizedDescription}");
                    tcs.TrySetException(new UserCancelledException("User cancelled Apple ID authorization"));
                }
                else
                {
                    Debug.LogError($"[AppleAuth] Login failed: {error.LocalizedDescription}");
                    tcs.TrySetException(new Exception(error.LocalizedDescription));
                }
            });
            
            while (!tcs.Task.IsCompleted)
            {
                appleAuthManager.Update();
                await UniTask.Yield();
            }
            
            var appleIdCredential = await tcs.Task;
            var rawBytes = appleIdCredential.IdentityToken;
            if (rawBytes == null || rawBytes.Length == 0)
            {
                Debug.LogError("[AppleAuth] IdentityToken is empty!");
            }
            var idToken = System.Text.Encoding.UTF8.GetString(appleIdCredential.IdentityToken);
            
            var accessToken = appleIdCredential.AuthorizationCode != null 
                ? Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode) 
                : null;

            return AuthCredentialBuilder
                .For(AuthType)
                .WithIdToken(idToken)
                .WithRawNonce(rawNonce)
                .WithAccessToken(accessToken)
                .WithUsername(appleIdCredential.FullName?.GivenName ?? "")
                .Build();
        }

        private static bool IsUserCancellation(IAppleError error)
        {
            // Check common Apple ID cancellation patterns
            var description = error.LocalizedDescription?.ToLower() ?? "";

            // Common Apple ID authorization cancellation indicators:
            // - Error code 1001 (user cancelled)
            // - Description contains "cancel" or similar words
            // - Description contains "user" + "cancel" variations
            return description.Contains("cancel") ||
                   description.Contains("cancelled") ||
                   description.Contains("user canceled") ||
                   description.Contains("user cancelled") ||
                   description.Contains("1001") ||
                   description.Contains("abort") ||
                   description.Contains("dismissed") ||
                   description.Contains("declined");
        }

        private static string ComputeSHA256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
#endif
