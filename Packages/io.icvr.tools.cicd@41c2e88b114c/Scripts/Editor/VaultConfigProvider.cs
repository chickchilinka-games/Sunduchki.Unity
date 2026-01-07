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

using ICVR.Tools.Vault.Services;

namespace ICVR.Tools
{
    public class VaultConfigProvider : IConfigProvider
    {
        private readonly VaultService _vaultService;

        public VaultConfigProvider(VaultService vaultService)
        {
            _vaultService = vaultService;
        }
        
        public TData GetData<TData>() where TData : class
        {
            return _vaultService.GetData<TData>();
        }

        public string GetRawData()
        {
            return _vaultService.GetRawData();
        }

        public bool CreateFileFromProperty(string propertyName, string resultFilePath)
        {
            return _vaultService.CreateFileFromProperty(propertyName, resultFilePath);
        }
    }
}