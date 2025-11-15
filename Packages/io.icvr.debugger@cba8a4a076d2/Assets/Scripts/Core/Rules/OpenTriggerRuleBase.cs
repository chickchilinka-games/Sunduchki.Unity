// ICVR CONFIDENTIAL
// __________________
// 
// [2016] -  [2024] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to ICVR LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using System;
using Core.Data;
using Core.Rules;
using Core.Services;
using R3;
using UnityEngine;
using Zenject;

internal abstract class OpenTriggerRuleBase : IRule, IInitializable, IDisposable
{
    private InternalDebuggerService _debuggerService;
    private OpenTriggerData _triggerData;
    private IDisposable _clickStream;

    [Inject]
    private void Construct(InternalDebuggerService debuggerService, OpenTriggerData triggerData)
    {
        _triggerData = triggerData;
        _debuggerService = debuggerService;
    }

    public void Initialize()
    {
        var clickEmitter = Observable.EveryUpdate()
            .Where(_ => IsProperClick());

        _clickStream = clickEmitter.Select(_=> DateTime.Now).Chunk(_triggerData.TapsCount, 1)
            .Where(clicks => (clicks[^1] - clicks[0]).Seconds < 1)
            .Subscribe(_ => _debuggerService.ShowDebugger());
    }

    protected abstract bool IsProperClick();

    protected bool HasHitTrigger(Vector2 screenPoint)
    {
        var triggerPosition = new Vector2(Screen.width, Screen.height);
        triggerPosition.Scale(_triggerData.Position);
        triggerPosition += _triggerData.PixelOffset;
        var triggerMin = triggerPosition - _triggerData.PixelSize / 2;
        var triggerMax = triggerPosition + _triggerData.PixelSize / 2;
        return screenPoint.x >= triggerMin.x && screenPoint.y >= triggerMin.y &&
               screenPoint.x <= triggerMax.x && screenPoint.y <= triggerMax.y;
    }

    public void Dispose()
    {
        _clickStream?.Dispose();
    }
}
