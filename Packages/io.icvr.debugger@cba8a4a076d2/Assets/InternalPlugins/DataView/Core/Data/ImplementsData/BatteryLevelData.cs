using System.Globalization;
using UnityEngine.Device;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal class BatteryLevelData : SimpleUpdatableByTimeData
    {
        private const string Name = "BatteryLevel";
        private const int DelayUpdateSeconds = 10 * 60;

        public BatteryLevelData() : base(Name, DelayUpdateSeconds)
        {
        }

        protected override void UpdateData()
        {
            InnerContent.Value = SystemInfo.batteryLevel.ToString(CultureInfo.InvariantCulture);
        }
    }
}