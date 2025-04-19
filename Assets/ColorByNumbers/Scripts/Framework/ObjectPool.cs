using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ObjectPool
	{
		#region Member Variables

		private GameObject			objectPrefab		= null;
		private List<GameObject>	instantiatedObjects = new List<GameObject>();
		private Transform			parent				= null;

		#endregion

		#region Public Methods

		/// <summary>
		/// Initializes a new instance of the ObjectPooler class.
		/// </summary>
		public ObjectPool(GameObject objectPrefab, int initialSize, Transform parent = null)
		{
			this.objectPrefab	= objectPrefab;
			this.parent			= parent;

			for (int i = 0; i < initialSize; i++)
			{
				GameObject obj = CreateObject();
				obj.SetActive(false);
			}
		}

		public static Transform CreatePoolContainer(Transform containerParent)
		{
			GameObject container = new GameObject("pool_container");

			container.transform.SetParent(containerParent);

			return container.transform;
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject()
		{
			GameObject obj = null;

			for (int i = 0; i < instantiatedObjects.Count; i++)
			{
				if (!instantiatedObjects[i].activeSelf)
				{
					obj = instantiatedObjects[i];
					break;
				}
			}

			if (obj == null)
			{
				obj = CreateObject();
			}

			obj.SetActive(true);

			return obj;
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject(Transform parent)
		{
			GameObject obj = GetObject();

			obj.transform.SetParent(parent, false);

			return obj;
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>(Transform parent) where T : Component
		{
			return GetObject(parent).GetComponent<T>();
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>() where T : Component
		{
			return GetObject().GetComponent<T>();
		}

		/// <summary>
		/// Sets all instantiated GameObjects to de-active
		/// </summary>
		public void ReturnAllObjectsToPool()
		{
			for (int i = 0; i < instantiatedObjects.Count; i++)
			{
				ReturnObjectToPool(instantiatedObjects[i]);
			}
		}

		/// <summary>
		/// Returns the object to pool.
		/// </summary>
		public void ReturnObjectToPool(GameObject obj)
		{
			obj.SetActive(false);
			obj.transform.SetParent(parent, false);
		}

		/// <summary>
		/// Destroies all objects.
		/// </summary>
		public void DestroyAllObjects()
		{
			for (int i = 0; i < instantiatedObjects.Count; i++)
			{
				GameObject.Destroy(instantiatedObjects[i]);
			}

			instantiatedObjects.Clear();
		}

		#endregion

		#region Private Methods

		private GameObject CreateObject()
		{
			GameObject obj = GameObject.Instantiate(objectPrefab);
			obj.transform.SetParent(parent, false);
			instantiatedObjects.Add(obj);
			return obj;
		}

		#endregion
	}
}
