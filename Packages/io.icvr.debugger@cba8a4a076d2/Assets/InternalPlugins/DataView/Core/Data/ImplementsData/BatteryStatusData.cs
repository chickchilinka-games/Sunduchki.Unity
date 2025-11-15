using UnityEngine.Device;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal class BatteryStatusData : SimpleUpdatableByTimeData
    {
        private const string Name = "BatteryStatus";
        private const int DelayUpdateSeconds = 10 * 60;

        public BatteryStatusData() : base(Name, DelayUpdateSeconds)
        {
        }

        protected override void UpdateData()
        {
            InnerContent.Value = SystemInfo.batteryStatus.ToString();
        }
    }
}