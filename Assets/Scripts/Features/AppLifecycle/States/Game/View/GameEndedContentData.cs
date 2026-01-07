using ICVR.Window.Basics;

namespace Features.AppLifecycle.States.Game.View
{
    public readonly struct GameEndedContentData : IWindowData
    {
        public bool IsWin { get; }
        public string Reason { get; }

        public GameEndedContentData(bool isWin, string reason)
        {
            IsWin = isWin;
            Reason = reason;
        }
    }
}
