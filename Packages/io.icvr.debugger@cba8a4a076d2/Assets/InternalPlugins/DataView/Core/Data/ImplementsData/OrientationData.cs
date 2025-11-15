using DebuggerPlugins.TestDataView.Core.Data;
using UnityEngine;
using Zenject;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal class OrientationData : StringData, ITickable
    {
        private const string Name = "Orientation";

        public OrientationData() : base(Name)
        {
        }

        public void Tick()
        {
            InnerContent.Value = Input.deviceOrientation.ToString();
        }
    }
}