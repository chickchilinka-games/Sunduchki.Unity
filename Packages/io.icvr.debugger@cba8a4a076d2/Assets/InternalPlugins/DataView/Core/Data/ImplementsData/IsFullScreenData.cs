using UnityEngine.Device;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal class IsFullScreenData : SimpleUpdatableByTimeData
    {
        private const string Name = "IsFullScreen";
        private const int DelayUpdateSeconds = 1;
        private double deltaTime;

        public IsFullScreenData() : base(Name, DelayUpdateSeconds)
        {
        }

        protected override void UpdateData()
        {
            InnerContent.Value = Screen.fullScreen.ToString();
        }
    }
}