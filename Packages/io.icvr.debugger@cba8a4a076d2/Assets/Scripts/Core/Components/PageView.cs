namespace Core.Components
{
    internal class PageView : ICVRDebuggerPage
    {
        public override bool DisplayPage(string id)
        {
            return DisplayLayoutById(id);
        }
    }
}