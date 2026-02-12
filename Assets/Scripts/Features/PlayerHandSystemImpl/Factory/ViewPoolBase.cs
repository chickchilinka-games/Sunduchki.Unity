using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.Factory
{
    /// <summary>
    /// Provides common transform setup and lifecycle management for pooled views.
    /// </summary>
    /// <typeparam name="TData">Type of the initialization data</typeparam>
    /// <typeparam name="TView">Type of the view component being pooled</typeparam>
    public abstract class ViewPoolBase<TData, TView> : MemoryPool<Transform, TData, TView>
        where TView : Component
    {
        protected sealed override void Reinitialize(Transform parent, TData data, TView item)
        {
            // Apply common transform setup
            SetupTransform(item, parent);

            // Allow derived classes to perform custom initialization
            OnViewReinitialize(data, item);
        }

        protected sealed override void OnDespawned(TView item)
        {
            // Apply common cleanup
            item.gameObject.SetActive(false);

            // Allow derived classes to perform custom cleanup
            OnViewDespawned(item);
        }

        /// <summary>
        /// Sets up common transform properties for the pooled view.
        /// </summary>
        /// <param name="item">The view item to setup</param>
        /// <param name="parent">The parent transform</param>
        private static void SetupTransform(TView item, Transform parent)
        {
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(true);
            item.transform.localScale = Vector3.one;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Override this method to perform view-specific initialization logic.
        /// Called after common transform setup is complete.
        /// </summary>
        /// <param name="data">Initialization data</param>
        /// <param name="item">The view item being reinitialized</param>
        protected abstract void OnViewReinitialize(TData data, TView item);

        /// <summary>
        /// Override this method to perform view-specific cleanup logic.
        /// Called after common cleanup is complete.
        /// </summary>
        /// <param name="item">The view item being despawned</param>
        protected virtual void OnViewDespawned(TView item)
        {
            // Default implementation does nothing
        }
    }
}
