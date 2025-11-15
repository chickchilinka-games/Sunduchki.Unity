using System;
using Core.Components;
using UnityEngine;

namespace Core.Data
{
    internal class ViewTemplateConfig : ScriptableObject
    {
        [SerializeField]
        private GameObject _debuggerWindowPrefab;
        public GameObject DebuggerWindowPrefab => _debuggerWindowPrefab;
    }
}