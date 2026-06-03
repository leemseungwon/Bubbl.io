using System;
using System.Collections.Generic;
using System.Linq;
using _02._Scripts.Environment;
using UnityEngine;

namespace _02._Scripts.Entity
{
    public class Entity : MonoBehaviour
    {
        [field:SerializeField] public GridBall OwnGrid { get; private set; }
        
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

        public bool GetCompo<T>(out T compo)
        {
            compo = default(T);
            if (Components.TryGetValue(typeof(T), out IEntityComponent component) 
                && component is T bindingCompo)
            {
                compo = bindingCompo;
                return true;
            }
            return false;
        }
    }
}