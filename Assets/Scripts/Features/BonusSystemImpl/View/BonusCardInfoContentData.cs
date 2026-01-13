using ICVR.Window.Basics;

namespace Features.BonusSystemImpl.View
{
    public readonly struct BonusCardInfoContentData : IWindowData
    {
        public UnityEngine.Sprite Icon { get; }
        public string Title { get; }
        public string Description { get; }

        public BonusCardInfoContentData(UnityEngine.Sprite icon, string title, string description)
        {
            Icon = icon;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
