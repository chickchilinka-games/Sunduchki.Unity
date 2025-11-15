using System.Globalization;
using DebuggerPlugins.TestDataView.Core.Data;
using UnityEngine;
using Zenject;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal class FpsData : StringData, ITickable
    {
        private const string Name = "Fps";
        private double deltaTime;

        public FpsData() : base(Name)
        {
        }

        public void Tick()
        {
            deltaTime += Time.deltaTime;
            deltaTime /= 2.0;
            InnerContent.Value = (1.0 / deltaTime).ToString("###.#");
        }
    }
}