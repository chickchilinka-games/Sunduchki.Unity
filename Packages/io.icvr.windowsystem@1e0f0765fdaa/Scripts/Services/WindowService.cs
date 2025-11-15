// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Basics;
using ICVR.Window.Interfaces;
using ICVR.Window.Models;
using ICVR.Window.Spawners;
using UnityEngine;

namespace ICVR.Window.Services
{
    internal class WindowService
    {
        private readonly WindowContentPool _contentPool;
        private readonly WindowTemplatePool _templatePool;
        private readonly WindowFactory _windowFactory;
        private readonly Queue<IWindow> _windowQueue = new Queue<IWindow>();
        private readonly WindowSystemModel _windowSystemModel;

        public WindowService(WindowFactory windowFactory,
            WindowContentPool contentPool,
            WindowTemplatePool templatePool,
            WindowSystemModel windowSystemModel)
        {
            _windowFactory = windowFactory;
            _contentPool = contentPool;
            _templatePool = templatePool;
            _windowSystemModel = windowSystemModel;
        }
        
        public UniTask<string> ShowWindowAsync<TContent>(string templateId, bool waitOthersClose)
            where TContent : AbstractContent
        {
            var contentId = _contentPool.GetContentIdByType(typeof(TContent));
            
            return ShowWindowAsync(contentId, templateId, waitOthersClose);
        }
        public UniTask<string> ShowWindowAsync(string contentId, string templateId, bool waitOthersClose)
        {
            if (!MakeSureWindowCanBeShown(contentId))
            {
                return UniTask.FromResult(string.Empty);
            }

            var contentTask = _contentPool.Spawn(contentId, null);
            var templateTask = _templatePool.Spawn(templateId, null);

            return ShowWindowAsync(contentTask, templateTask, waitOthersClose);
        }

        public UniTask<string> ShowWindowAsync<TContent, TData>(string templateId, TData data, bool waitOthersClose)
            where TContent : AbstractContentWithData<TData>
            where TData : IWindowData
        {
            var contentId = _contentPool.GetContentIdByType(typeof(TContent));
            
            return ShowWindowAsync(contentId, templateId, data, waitOthersClose);
        }
        
        public UniTask<string> ShowWindowAsync<TData>(string contentId, string templateId, TData data, bool waitOthersClose)
            where TData : IWindowData
        {
            if (!MakeSureWindowCanBeShown(contentId))
            {
                return UniTask.FromResult(string.Empty);
            }

            var contentTask = _contentPool.Spawn(contentId, new object[]{data});
            var templateTask = _templatePool.Spawn(templateId, null);

            return ShowWindowAsync(contentTask, templateTask, waitOthersClose);
        }

        private async UniTask<string> ShowWindowAsync(UniTask<IContent> contentTask, UniTask<ITemplate> templateTask, bool waitOthersClose)
        {
            var (content, template) = await UniTask.WhenAll(contentTask, templateTask);

            var window = _windowFactory.Create(content, template);
            
            SubscribeListenersOnClose(() => CloseWindowAsync(window), content, template);

            _windowQueue.Enqueue(window);
            if (waitOthersClose)
            {
                var nextWindow = _windowQueue.Peek();

                if (nextWindow.IsShown)
                {
                    return nextWindow.Id;
                }
                await nextWindow.Show();
            }
            else
            {
                foreach (var currentWindow in _windowQueue)
                {
                    if (currentWindow != window) 
                        currentWindow.Hide().Forget();
                }
                await window.Show();
            }

            return window.Id;
        }
        
        private bool MakeSureWindowCanBeShown(string contentId)
        {
            if (_windowSystemModel.CurrentWindowArea == null)
            {
                Debug.LogError("No holders were found! Make sure you added one into current scene.");
                
                return false;
            }
            
            if (IsWindowWithContentOpen(contentId))
            {
                Debug.LogWarning($"Window with content {contentId} has already opened.");
                
                return false;
            }
            
            return true;
        }

        private static void SubscribeListenersOnClose(Action closeAction, params ICloseListener[] listeners)
        {
            Action unsubscribeClose = () =>
            {
                foreach (var listener in listeners)
                {
                    listener?.ClearCloseSubscriptions();
                }
            };
            
            foreach (var listener in listeners)
            {
                listener.SubscribeOnClose(closeAction);
                listener.SubscribeOnClose(unsubscribeClose);
            }
        }

        public async UniTask CloseAllWindows()
        {
            var allWindows = _windowQueue.ToList();

            if (!allWindows.Any())
            {
                return;
            }
            
            foreach (var window in allWindows)
            {
                await CloseWindowAsync(window.Id);
            }
        }
        
        public void CloseWindow(string windowId)
        {
            CloseWindowAsync(windowId).Forget();
        }

        public UniTask CloseWindowAsync(string windowId)
        {
            var window = GetWindowsFromQueue(windowId);
            
            return CloseWindowAsync(window);
        }

        public void CloseWindowsWithContent<TContent>() where TContent : AbstractContent => 
            CloseWindowsWithContentAsync<TContent>().Forget();

        public UniTask CloseWindowsWithContentAsync<TContent>() where TContent : AbstractContent
        {
            return CloseWindowsAsync(window => window.Content is TContent);
        }
        
        public void CloseWindowsWithTemplate<TTemplate>() where TTemplate : AbstractTemplate => 
            CloseWindowsWithTemplateAsync<TTemplate>().Forget();

        public UniTask CloseWindowsWithTemplateAsync<TTemplate>() where TTemplate : AbstractTemplate
        {
            return CloseWindowsAsync(window => window.Template is TTemplate);
        }

        private async UniTask CloseWindowAsync(IWindow window)
        {
            if (window == null)
            {
                return;
            }

            var currentWindow = GetWindowsFromQueue(window.Id, true);
            
            if (_windowQueue.TryPeek(out var nextWindow))
            {
                currentWindow.Hide().ContinueWith(()=> currentWindow.Dispose()).Forget();
                await nextWindow.Show();
            }
            else
            {
                await currentWindow.Hide();
                currentWindow.Dispose();
            }
        }
        
        internal async UniTask CloseWindowsAsync(Func<IWindow,bool> predicate)
        {
            var windows = GetWindowsFromQueue(predicate, true);

            if (windows.Count == 0)
            {
                return;
            }
            
            await UniTask.WhenAll(windows.Select(window => window.Hide().ContinueWith(window.Dispose)));

            if (_windowQueue.TryPeek(out var nextWindow))
            {
                await nextWindow.Show();
            }
        }

        public bool IsWindowOpen(string windowId)
        {
            var window = GetWindowsFromQueue(windowId);
            
            return window != null;
        }

        private bool IsWindowWithContentOpen(string contentId)
        {
            var windows = GetWindowsFromQueue(window =>
            {
                var thisContentId = _contentPool.GetContentIdByType(window.Content.GetType());
                
                return contentId.Equals(thisContentId);
            });
            
            return windows.Any();
        }
        
        public bool IsWindowOpen<TContent>() where TContent : AbstractContent
        {
            var windows = GetWindowsFromQueue(window => window.Content is TContent);
            
            return windows.Any();
        }

        public bool CanShowWindows()
        {
            return _windowSystemModel.CurrentWindowArea;
        }

        public bool CanShowContent<TContent>() where TContent : AbstractContent
        {
            return _contentPool.CanSpawn<TContent>();
        }

        private IWindow GetWindowsFromQueue(string id, bool dequeue = false)
        {
            var dequeuedWindows = new List<IWindow>();

            IWindow targetWindow = null;
            
            while (_windowQueue.Count > 0)
            {
                var window = _windowQueue.Dequeue();

                if (window.Id.Equals(id))
                {
                    targetWindow = window;

                    if (!dequeue)
                    {
                        dequeuedWindows.Add(window);
                    }
                    
                    break;
                }
                
                dequeuedWindows.Add(window);
            }

            dequeuedWindows.Reverse();
            
            foreach (var window in dequeuedWindows)
            {
                _windowQueue.Enqueue(window);
            }

            return targetWindow;
        }
        
        private List<IWindow> GetWindowsFromQueue(Func<IWindow,bool> predicate, bool dequeue = false)
        {
            var dequeuedWindows = new List<IWindow>();
            var targetWindows = new List<IWindow>();
            
            while (_windowQueue.Count > 0)
            {
                var window = _windowQueue.Dequeue();

                if (predicate.Invoke(window))
                {
                    targetWindows.Add(window);

                    if (!dequeue)
                    {
                        dequeuedWindows.Add(window);
                    }
                    
                    continue;
                }
                
                dequeuedWindows.Add(window);
            }

            dequeuedWindows.Reverse();
            
            foreach (var window in dequeuedWindows)
            {
                _windowQueue.Enqueue(window);
            }

            return targetWindows;
        }
    }
}