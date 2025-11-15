namespace Core.Components
{
    internal class WidgetView : ICVRDebuggerWidget
    {
        public override bool DisplayWidget(string id)
        {
            return DisplayLayoutById(id);
        }
    }
}