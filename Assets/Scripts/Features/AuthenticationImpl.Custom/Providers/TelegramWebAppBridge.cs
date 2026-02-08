using System.Runtime.InteropServices;
using UnityEngine;

namespace Modules.AuthenticationSystem.Providers
{
    public static class TelegramWebAppBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string TelegramWebAppGetInitData();
#endif

        public static string GetInitData()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return TelegramWebAppGetInitData();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TelegramWebAppBridge] Failed to retrieve initData: {ex.Message}");
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
