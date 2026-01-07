// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ICVR.Tools.Vault.Utils
{
    internal class WebUtils
    {
        public static string Post(string url, 
                                                Dictionary<string, string> parameters,
                                                CancellationToken cancellationToken,
                                                Dictionary<string, string> headers = null,
                                                uint timeout = 10)
        {
            var postRequest = UnityWebRequest.Post(url, parameters);
            var result = ProcessRequest(postRequest, cancellationToken, headers, timeout);
            
            postRequest.Dispose();

            return result;
        }

        public static string Get(string url,
                                            CancellationToken cancellationToken,
                                            Dictionary<string, string> headers = null,
                                            uint timeout = 10)
        {
            var getRequest = UnityWebRequest.Get(url);
            var result = ProcessRequest(getRequest, cancellationToken, headers, timeout);
            
            getRequest.Dispose();

            return result;
        }

        private static string ProcessRequest(UnityWebRequest webRequest,
                                                        CancellationToken cancellationToken,
                                                        Dictionary<string, string> headers = null,
                                                        uint timeout = 10)
        {
            if (headers != null && headers.Any())
            {
                SetHeaders(ref webRequest, headers);
            }

            SendRequest(webRequest, timeout, cancellationToken);
            
            var resultText = webRequest.downloadHandler.text;

            if (!string.IsNullOrEmpty(webRequest.downloadHandler.error))
            {
                Debug.Log($"[Vault] Error: {webRequest.downloadHandler.error}");
            }
            
            return resultText;
        }

        private static bool SendRequest(UnityWebRequest webRequest, uint timeout, CancellationToken cancellationToken)
        {
            var op = webRequest.SendWebRequest();
            var timer = (float)timeout;
            var timeSpan = TimeSpan.FromSeconds(Time.fixedDeltaTime);            
            
            while (!op.isDone && timer > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                Thread.Sleep(timeSpan);

                timer -= Time.fixedDeltaTime;
            }

            return webRequest.result == UnityWebRequest.Result.Success;
        }
        
        private static void SetHeaders(ref UnityWebRequest webRequest, Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);   
            }
        }
    }
}