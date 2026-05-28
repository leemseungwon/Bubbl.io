using System.Collections.Generic;
using _02._Scripts.Enum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02._Scripts.Core.Managers
{
    [DefaultExecutionOrder(-102)]
    public class Manager : MonoSingleton<Manager>
    {
        private readonly List<IManager> _managerList = new List<IManager>();

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            
            Initialize();
            SceneManager.sceneLoaded += SceneLoad;
        }

        private void Initialize()
        {
            IManager[] managers = GetComponentsInChildren<IManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                if(managers[i] == null)
                    continue;
            
                _managerList.Add(managers[i]);
                managers[i].Initialize(this);
            }
        }

        public void SceneLoad(Scene loadedScene, LoadSceneMode _)
        {
            for (int i = 0; i < _managerList.Count; i++)
            {
                if(_managerList[i] == null)
                    continue;
                
                _managerList[i].LoadScene((SceneType)loadedScene.buildIndex);
            }
        }
    
        public T GetManager<T>() where T : class, IManager
        {
            return _managerList.Find(x => x is T) as T;
        }
    
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < _managerList.Count; i++)
            {
                _managerList[i].Reset();
            }
            SceneManager.sceneLoaded -= SceneLoad;
        }
    }
}