using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using UnityEngine;

namespace Core.Components
{
    internal class ICVRLayoutDrawer : MonoBehaviour
    {
        [field:SerializeField] public RectTransform LayoutContainer { get; private set; }

        protected Dictionary<string, ILayout> LayoutsDictionary;
        protected ILayout CurrentLayout;

        public virtual void Initialize(IReadOnlyList<ILayout> layouts)
        {
            CurrentLayout = null;
            LayoutsDictionary = layouts.ToDictionary(layout => layout.Id);
        }

        protected bool DisplayLayoutById(string id)
        {
            bool success = true;
            ILayout targetToDisplay = null;

            bool stringNotEmpty = !string.IsNullOrEmpty(id);
            if (stringNotEmpty && !LayoutsDictionary.TryGetValue(id, out targetToDisplay))
            {
                Debug.LogError($"[Debugger] Layout not found: {id}");
                success = false;
            }

            DisplayLayout(targetToDisplay);
            return success;
        }

        private void DisplayLayout(ILayout target)
        {
            if (CurrentLayout != null)
            {
                CurrentLayout.Pivot.gameObject.SetActive(false);
            }

            if (target != null)
            {
                target.Pivot.gameObject.SetActive(true);
            }

            CurrentLayout = target;
        }
    }
}