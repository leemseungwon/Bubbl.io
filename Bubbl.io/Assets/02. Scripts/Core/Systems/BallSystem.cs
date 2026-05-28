using System;
using System.Collections.Generic;
using System.Linq;
using _02._Scripts.Ball;
using _02._Scripts.Core.Managers;
using _02._Scripts.Entity;
using UnityEngine;

namespace _02._Scripts.Core.Systems
{
    public class BallSystem : MonoBehaviour, ISystem
    {
        [SerializeField] private List<BallMaterialData> ballMaterials = new List<BallMaterialData>();
        
        private PoolingManager _poolingManager;

        public void Initialize(System system)
        {
            _poolingManager = Manager.Instance.GetManager<PoolingManager>();
        }

        public IBall CreateBall(BallColorType type, Transform spawnPoint)
        {
            if (_poolingManager == null)
            {
                return null;
            }
            
            if (_poolingManager.SpawnPool("Ball", spawnPoint, out CommonBall spawnedBall))
            {
                spawnedBall.SetColor(type);
                return spawnedBall;
            }
            
            return null;
        }
        
        public void Reset() { }
        
        public Material GetMaterial(BallColorType colorType)
            => ballMaterials.FirstOrDefault(x => x.colorType == colorType).material;
    }
    
    [Serializable]
    public struct BallMaterialData
    {
        public BallColorType colorType;
        public Material material;
    }
}