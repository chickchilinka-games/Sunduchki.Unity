using UnityEngine;
using Zenject;
using IPoolable = Pool.IPoolable;

namespace Base.Pool.Factory
{
	public class Pool<TPoolable> : MemoryPool<TPoolable> 
		where TPoolable : IPoolable
	{
		public TPoolable Spawn(UnityEngine.Transform parent)
		{
			var poolable = Spawn();
			poolable.OnSpawn(parent);
		
			return poolable;
		}

		protected override void Reinitialize(TPoolable item)
		{
			item.ReInitialize();
		}

		protected override void OnDestroyed(TPoolable item)
		{
			Object.Destroy(item.GameObject);
		}

		public void Despawn(TPoolable poolable, UnityEngine.Transform parent)
		{
			if (IsInactiveItemsContainsPoolable(poolable))
			{
				Log("Tried to return an item to pool twice. Ignored!");
				return;
			}
			
			poolable.OnDespawn(parent);
			base.Despawn(poolable);
		}

		public void DestroyAll()
		{
			Clear();
		}

		public override string ToString()
		{
			return typeof(TPoolable).Name;
		}

		private bool IsInactiveItemsContainsPoolable(TPoolable poolable)
		{
			foreach (var inactiveItem in InactiveItems)
			{
				if (inactiveItem.Equals(poolable))
					return true;
			}

			return false;
		}

		private void Log(string msg)
		{
			Debug.LogWarning($"[Pool<{typeof(TPoolable).Name}>] " + msg);
		}
	}
}