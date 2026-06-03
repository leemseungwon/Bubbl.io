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
        [SerializeField] private List<BallMaterialData> ballMaterials = new List<BallMaterialData>{
            new BallMaterialData{colorType = BallColorType.Red, material = null},
            new BallMaterialData{colorType = BallColorType.Blue, material = null},
            new BallMaterialData{colorType = BallColorType.Green, material = null},
            new BallMaterialData{colorType = BallColorType.Pink, material = null},
            new BallMaterialData{colorType = BallColorType.Brown, material = null},
        };
        
        private PoolingManager _poolingManager;
        
        public void Initialize(System system)
        {
            _poolingManager = Manager.Instance.GetManager<PoolingManager>();
        }

        public CommonBall CreateBall(Entity.Entity owner, BallColorType type)
        {
            if (_poolingManager == null)
            {
                return null;
            }
            
            if (_poolingManager.SpawnPool("Ball", out CommonBall spawnedBall))
            {
                spawnedBall.transform.position = Vector3.zero;
                spawnedBall.SpawnBall(owner, type);
                return spawnedBall;
            }
            
            Debug.LogError("🚨 BallSystem: 풀 매니저에서 CommonBall을 꺼내는 데 실패했습니다.");
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