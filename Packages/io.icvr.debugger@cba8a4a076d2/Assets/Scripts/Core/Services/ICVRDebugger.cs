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

using Core.Interfaces;

namespace Core.Services
{
    public class ICVRDebugger
    {
        private readonly ILogger _logger;
        private readonly InternalDebuggerService _debuggerService;

        internal ICVRDebugger(ILogger logger, InternalDebuggerService debuggerService)
        {
            _debuggerService = debuggerService;
            _logger = logger;
        }

        public string GetLog(string[] categoryFilter = null)
        {
            return _logger.GetLog(categoryFilter);
        }

        public void ShowDebugger()
        {
            _debuggerService.ShowDebugger();
        }
        
        public void HideDebugger()
        {
            _debuggerService.HideDebugger();
        }
    }
}