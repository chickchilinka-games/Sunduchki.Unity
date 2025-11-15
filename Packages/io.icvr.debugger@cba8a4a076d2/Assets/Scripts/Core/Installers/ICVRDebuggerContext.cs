using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    [Serializable]
    [RequireComponent(typeof(ProjectContext))]
    internal class ICVRDebuggerContext : MonoBehaviour
    {
        private ProjectContext _projectContext;
        
        protected ProjectContext ProjectContext
        {
            get
            {
                if (_projectContext == null)
                {
                    _projectContext = GetComponent<ProjectContext>();
                }
                
                return _projectContext;
            }
        }

        public IEnumerable<ScriptableObjectInstaller> ScriptableObjectInstallers =>
            ProjectContext.ScriptableObjectInstallers;
        
        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void AddMainInstaller(DebuggerInstaller mainInstaller)
        {
            var installers = ProjectContext.ScriptableObjectInstallers.ToList();
            installers.Add(mainInstaller);
            
            ProjectContext.ScriptableObjectInstallers = installers;
        }
        
        public void AddPlugins(IEnumerable<DebuggerPluginInstaller> newPlugins)
        {
            var installers = ProjectContext.ScriptableObjectInstallers.ToList();
            installers.AddRange(newPlugins);
            
            ProjectContext.ScriptableObjectInstallers = installers;
        }

        public void ClearMainInstaller()
        {
            ProjectContext.ScriptableObjectInstallers = ProjectContext.ScriptableObjectInstallers
                .Where(item => item is not DebuggerInstaller);
        }
        
        public void ClearPlugins()
        {
            ProjectContext.ScriptableObjectInstallers = ProjectContext.ScriptableObjectInstallers
                .Where(item => item is not DebuggerPluginInstaller);
        }
    }
}