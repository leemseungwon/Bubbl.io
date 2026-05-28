using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _02._Scripts.Entity
{
    public class Entity : MonoBehaviour
    {
        protected Dictionary<Type, IEntityComponent> Components;
        
        protected virtual void Awake()
        {
            Components = GetComponentsInChildren<IEntityComponent>(true)
                .ToDictionary(compo => compo.GetType());
            
            InitComponents();
        }
        
        protected virtual void InitComponents()
        {
            Components.Values.ToList().ForEach(component => component.Initialize(this));
        }
        
        public T GetCompo<T>()
        {
            if (Components.TryGetValue(typeof(T), out IEntityComponent component) 
                && component is T compo)
            {
                return compo;
            }

            IEntityComponent findComponent = Components.Values.FirstOrDefault(c => c is T);
            if (findComponent is T findCompo)
                return findCompo;
            Debug.LogError($"{typeof(T).ToString()} Compo is Null");
            return default(T);
        }
    }
}