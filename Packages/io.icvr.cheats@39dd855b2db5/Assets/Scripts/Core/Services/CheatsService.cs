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

using System.Collections.Generic;
using Cheats.Core.Interfaces;
using Cheats.Core.Storages;

namespace Cheats.Core.Services
{
    public class CheatsService
    {
        private CheatsModelStorage _cheatsModelStorage;
        
        internal CheatsService(CheatsModelStorage cheatsModelStorage)
        {
            _cheatsModelStorage = cheatsModelStorage;
        }
        
        public void AddCheat(ICheat cheat)
        {
            _cheatsModelStorage.Add(cheat);
        }
        
        public ICheat GetCheatById(string id)
        {
            return _cheatsModelStorage.GetById(id);
        }

        public List<ICheat> GetAllCheat()
        {
            return _cheatsModelStorage.GetAll();
        }
    }
}