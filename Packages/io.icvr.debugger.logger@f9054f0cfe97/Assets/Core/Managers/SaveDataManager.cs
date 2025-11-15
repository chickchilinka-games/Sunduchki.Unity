using System;
using System.IO;
using UnityEngine;

namespace DebuggerPlugins.Logger.Managers
{
    internal class SaveDataManager
    {
        public void CopyToClipboard(string data)
        {
            GUIUtility.systemCopyBuffer = data;
        }

        public void SaveToFile(string data)
        {
            string destination = Application.persistentDataPath + $"/log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            using var file = new StreamWriter(destination);
            file.Write(data);
        }
    }
}