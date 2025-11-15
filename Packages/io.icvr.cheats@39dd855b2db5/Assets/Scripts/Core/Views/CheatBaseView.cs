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
using Cheats.Core.Interfaces;
using UnityEngine;

namespace Cheats.Core.Views
{
    public abstract class CheatBaseView : MonoBehaviour, ICheatView
    {
        public abstract string Type { get; }
        public abstract void BindTo(ICheat cheat);
        public virtual void Initialize(int order)
        {
            transform.SetSiblingIndex(order);
        }

    }
    public abstract class CheatBaseView<TCheat> : CheatBaseView where TCheat : ICheat
    {
        protected TCheat Cheat;
        public override void BindTo(ICheat cheat)
        {
            try
            {
                Cheat = (TCheat)cheat;
            }
            catch (Exception)
            {
                Debug.LogError( $"Failed to cast cheat {cheat.GetType().Name} to {typeof(TCheat)}", gameObject);
            }
        }
    }
    
    public abstract class CheatActionView : CheatBaseView<ICheatAction>
    {
    }
    
    public abstract class CheatViewWithParameters<T> : CheatBaseView<ICheatDataSource<T>>
    {
    }
    
    public abstract class CheatViewWithClampedParameters<T> : CheatBaseView<IClampedCheatDataSource<T>>
    {
    }
}