using System.Collections.Generic;
using UnityEngine;

namespace _02._Scripts.Core.Systems
{
    [DefaultExecutionOrder(-101)]
    public class System : MonoSingleton<System>
    {
        private readonly List<ISystem> _systemList = new List<ISystem>();

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            ISystem[] managers = GetComponentsInChildren<ISystem>();
            for (int i = 0; i < managers.Length; i++)
            {
                if(managers[i] == null)
                    continue;
            
                _systemList.Add(managers[i]);
            }
            _systemList.ForEach(system => system.Initialize(this));
        }
    
        public T GetSystem<T>() where T : class, ISystem
        {
            return _systemList.Find(x => x is T) as T;
        }
    
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < _systemList.Count; i++)
            {
                _systemList[i].Reset();
            }
        }
    }
}