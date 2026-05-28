using System;
using System.Collections.Generic;
using _02._Scripts.Enum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _02._Scripts.Core.Managers
{
    public class PoolingManager : MonoBehaviour, IManager
    {
        [SerializeField] private List<PoolingData> pool = new List<PoolingData>();

        private Dictionary<string, List<IPoolable>> _pooledObjects = new Dictionary<string, List<IPoolable>>();
        
        public void Initialize(Manager manager)
        {
            _pooledObjects.Clear();
            for (int i = 0; i < pool.Count; i++)
            {
                string poolName = pool[i].poolName;
                if(!_pooledObjects.TryAdd(poolName, InitializePool(poolName)))
                    Debug.LogError($"Pool {pool[i].poolName} already exists!");
            }
        }

        private List<IPoolable> InitializePool(string poolName)
        {
            List<IPoolable> spawnedPoolList = new List<IPoolable>();

            PoolingData data = pool.Find(p => p.poolName == poolName);
            
            Object spawnableObject = data.poolObject;
            
            if(spawnableObject == null)
            {
                Debug.LogError($"PoolObject {poolName} is not a Unity Object or is missing!");
                return spawnedPoolList;
            }
            
            for (int i = 0; i < 10; i++)
            {
                SpawnPool(poolName, transform, out IPoolable poolable);
                if (poolable != null)
                {
                    spawnedPoolList.Add(poolable);
                }
            }
            return spawnedPoolList;
        }
        
        private IPoolable CreateNewPoolable(Object prefab)
        {
            GameObject spawnedPool = Instantiate(prefab) as GameObject;
            if (spawnedPool == null) 
                return null;
            
            IPoolable poolable = spawnedPool.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.Initialize();
                spawnedPool.SetActive(false);
            }
            return poolable;
        }
        
        public bool SpawnPool<T>(string poolName, Transform spawnTrm, out T pooledObject) where T : IPoolable
        {
            pooledObject = default;
    
            if (!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList))
                return false;

            IPoolable target = null;

            for (int i = 0; i < poolableList.Count; i++)
            {
                if (poolableList[i] is Component comp && !comp.gameObject.activeSelf)
                {
                    target = poolableList[i];
                    break;
                }
            }

            if (target == null)
            {
                var data = pool.Find(p => p.poolName == poolName);
                if (data.poolObject != null)
                {
                    target = CreateNewPoolable(data.poolObject);
                    if (target != null) poolableList.Add(target);
                }
            }

            if (target != null && target is T castedTarget)
            {
                pooledObject = castedTarget;
        
                if (target is Component component)
                {
                    component.transform.position = spawnTrm.position;
                    component.transform.rotation = spawnTrm.rotation;
            
                    component.gameObject.SetActive(true);
                }
                return true;
            }
    
            return false;
        }

        public void DespawnPool(string poolName, IPoolable poolable)
        {
            if (!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList)) return;

            if (poolableList.Contains(poolable))
            {
                if (poolable is Component component)
                {
                    component.gameObject.SetActive(false);
                    component.transform.parent = transform;
                    
                    component.transform.localPosition = Vector3.zero;
                    component.transform.localRotation = Quaternion.identity;
                }
                poolable.Reset();
            }
        }
        
        public void LoadScene(SceneType sceneType)
        {
            
        }

        public void Reset()
        {
            foreach (var kvp in _pooledObjects)
            {
                foreach (var poolable in kvp.Value)
                {
                    Component component = poolable as Component;
                    
                    if (component != null) 
                    {
                        Destroy(component.gameObject);
                    }
                }
            }
            _pooledObjects.Clear();
        }
    }

    [Serializable]
    public struct PoolingData
    {
        public string poolName;
        public GameObject poolObject;
    }
}