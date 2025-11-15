using Core.Interfaces;
using UnityEngine;

namespace InternalPlugins.DataView.Core.Models
{
    public class DataViewPlugin : IPlugin
    {
        public string Title => "DataView";

        public DataViewPlugin()
        {
            Debug.Log("DataView used");
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }
    }
}