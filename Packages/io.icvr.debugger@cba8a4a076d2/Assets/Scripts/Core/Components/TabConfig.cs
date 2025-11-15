// File name: TabConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TabConfig", menuName = "Configs/TabConfig", order = 1)]
public class TabConfig : ScriptableObject
{
    [Serializable]
    public class TabEntry
    {
        public string LayoutId;
        public string TabName;
        public Sprite TabIcon;
    }
    [Header("Tab Settings")]
    public PageTab TabPrefab; // Reference to the Tab prefab
    public List<TabEntry> Tabs;
}