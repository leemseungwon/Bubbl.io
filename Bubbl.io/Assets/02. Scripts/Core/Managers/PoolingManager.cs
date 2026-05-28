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
                IPoolable poolable = CreateNewPoolable(spawnableObject, transform);
                if (poolable != null)
                {
                    spawnedPoolList.Add(poolable);
                }
            }
            return spawnedPoolList;
        }
        
        private IPoolable CreateNewPoolable(Object prefab, Transform spawnTrm)
        {
            GameObject spawnedPool = Instantiate(prefab, spawnTrm) as GameObject;
            if (spawnedPool == null) 
                return null;
            
            spawnedPool.transform.position = spawnTrm.position;
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
            
            if(!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList))
            {
                Debug.LogWarning($"{poolName} not found.");
                return false;
            }

            IPoolable target = poolableList.Find(p => p is Component comp && !comp.gameObject.activeSelf);

            if (target == null)
            {
                Object prefab = pool.Find(p => p.poolName == poolName).poolObject;
                target = CreateNewPoolable(prefab, spawnTrm);
                
                if (target != null)
                {
                    poolableList.Add(target);
                }
                else
                {
                    return false;
                }
            }

            if (target is Component component)
            {
                component.gameObject.SetActive(true);
            }
            
            pooledObject = (T)target;
            return true;
        }

        public void DespawnPool(string poolName, IPoolable poolable)
        {
            if (!_pooledObjects.TryGetValue(poolName, out List<IPoolable> poolableList)) return;

            if (poolableList.Contains(poolable))
            {
                if (poolable is Component component)
                {
                    component.gameObject.SetActive(false);
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