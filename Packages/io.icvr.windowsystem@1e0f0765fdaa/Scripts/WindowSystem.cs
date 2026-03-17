

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Basics;
using ICVR.Window.Services;

namespace ICVR.Window
{
    public class WindowSystem
    {
        private readonly WindowService _windowService;

        internal WindowSystem(WindowService windowService)
        {
            _windowService = windowService;
        }

        public UniTask<string> ShowWindowAsync(string contentId, string templateId, bool waitOthersClose = false)
        {
            return _windowService.ShowWindowAsync(contentId, templateId, waitOthersClose);
        }
        
        public UniTask<string> ShowWindowAsync<TContent>(string templateId, bool waitOthersClose = false) where TContent : AbstractContent
        {
            return _windowService.ShowWindowAsync<TContent>(templateId, waitOthersClose);
        }
        
        public UniTask<string> ShowWindowAsync<TContent, TData>(string templateId, TData data, bool waitOthersClose = false) 
            where TContent : AbstractContentWithData<TData>
            where TData : IWindowData
        {
            return _windowService.ShowWindowAsync<TContent, TData>(templateId, data, waitOthersClose);
        }

        public UniTask CloseAllWindows()
        {
            return _windowService.CloseAllWindows();
        }
        
        public void CloseWindow(string windowId) => _windowService.CloseWindow(windowId);

        public void CloseWindowsWithContent<TContent>() where TContent : AbstractContent
        {
            _windowService.CloseWindowsWithContent<TContent>();
        }

        public UniTask CloseWindowsWithContentAsync<TContent>() where TContent : AbstractContent
        {
            return _windowService.CloseWindowsWithContentAsync<TContent>();
        }

        public void CloseWindowsWithTemplate<TTemplate>() where TTemplate : AbstractTemplate
        {
            _windowService.CloseWindowsWithTemplate<TTemplate>();
        }

        public UniTask CloseWindowsWithTemplateAsync<TTemplate>() where TTemplate : AbstractTemplate
        {
            return _windowService.CloseWindowsWithTemplateAsync<TTemplate>();
        }

        public UniTask CloseWindowAsync(string windowId) => _windowService.CloseWindowAsync(windowId);
        
        public bool IsWindowOpen(string windowId) => _windowService.IsWindowOpen(windowId);
        
        public bool IsWindowOpen<TContent>() where TContent : AbstractContent
        {
            return _windowService.IsWindowOpen<TContent>();
        }
        
        public bool CanShowWindows() => _windowService.CanShowWindows();
        
        public bool CanShowContent<TContent>() where TContent : AbstractContent
        {
            return _windowService.CanShowContent<TContent>();
        }
    }
}