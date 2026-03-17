

using R3;

namespace ICVR.Window.Interfaces
{
    internal interface IContent : IIdentified, IChild, ICloseListener
    {
        string Title { get; }
        Observable<string> TitleChanged { get; }
    }
}