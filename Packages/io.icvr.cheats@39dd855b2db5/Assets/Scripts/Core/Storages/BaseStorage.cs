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
using Cheats.Core.Interfaces;
using R3;

namespace Cheats.Core.Storages
{
    public class BaseStorage<T> : IStorage<T>
    {
        public Observable<T> ElementAdded => _elementAdded;
        
        protected List<T> Elements = new();
        
        private readonly Subject<T> _elementAdded = new();

        public void Add(T element)
        {
            Elements.Add(element);
            _elementAdded.OnNext(element);
        }

        public List<T> GetAll()
        {
            return new List<T>(Elements);
        }
    }
}