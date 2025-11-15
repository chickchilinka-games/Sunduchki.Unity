using System;
using UnityEngine;

namespace Pool
{
    public class BasePoolable : MonoBehaviour, IPoolable
    {
        public virtual object ObjectId { get; private set; }
        
        public GameObject GameObject => gameObject;

        public void InitializePoolable(object objectId)
        {
            ObjectId = objectId;
        }

        public virtual void OnSpawn(Transform parent)
        {
            if (parent != null)
                transform.SetParent(parent, false);
        
            gameObject.SetActive(true);
        }

        public virtual void ReInitialize()
        {
            transform.localScale = Vector3.one;
        }

        public virtual void OnDespawn(Transform parent)
        {
            try
            {
                if (parent != null)
                    transform?.SetParent(parent, false);
            
                gameObject?.SetActive(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}