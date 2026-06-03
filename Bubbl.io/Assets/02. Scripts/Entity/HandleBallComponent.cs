using System.Collections.Generic;
using _02._Scripts.Ball;
using _02._Scripts.Core.Systems;
using UnityEngine;

namespace _02._Scripts.Entity
{
    public enum BallColorType
    {
        Red,
        Blue,
        Green,
        Pink,
        Brown,
    }
    
    public class HandleBallComponent : MonoBehaviour, IEntityComponent
    {
        [Header("Positions")]
        [SerializeField] private Transform currentSpawnPoint;

        private List<BallColorType> _baseBallBag = new List<BallColorType> 
        { 
            BallColorType.Red, BallColorType.Blue, BallColorType.Green, BallColorType.Pink, BallColorType.Brown
        };
        private List<BallColorType> _currentBag = new List<BallColorType>();
        private Queue<BallColorType> _colorDataQueue = new Queue<BallColorType>();
        private BallSystem _ballSystem;
        private Rigidbody _rigidbody;
        private Entity _owner;
        private bool _canLaunchBall = true;
        
        public CommonBall CurrentBall { get; private set; }

        public void Initialize(Entity entity)
        {
            _owner = entity;
            _rigidbody = GetComponent<Rigidbody>();
            _ballSystem = Core.Systems.System.Instance.GetSystem<BallSystem>(); 
        }
        
        public void Start()
        {
            RefillColorQueue();
            SpawnNextBall();
        }
        
        public void LaunchCurrentBall(Vector2 launchDirection)
        {
            if (CurrentBall == null || !_canLaunchBall) 
                return;
            
            _canLaunchBall = false;
            _owner.OwnGrid.IsAutoLowering = false;
            
            if (CurrentBall is Component ballComp)
            {
                ballComp.transform.SetParent(null);
            }

            CurrentBall.Launch(launchDirection);
            CurrentBall.OnSnapToGrid += SnapBallComplete;
        }

        private void SnapBallComplete()
        {
            if(CurrentBall != null)
                CurrentBall.OnSnapToGrid -= SnapBallComplete;
            
            _owner.OwnGrid.IsAutoLowering = true;
            SpawnNextBall();
            _owner.OwnGrid.IncreaseDifficulty();
            _canLaunchBall = true;
        }
        
        private void SpawnNextBall()
        {
            if (_ballSystem == null) 
                return;

            BallColorType nextColor = _colorDataQueue.Dequeue();
            RefillColorQueue();

            CurrentBall = _ballSystem.CreateBall(_owner, nextColor);
            AttachBallToPivot();
        }
        
        private void AttachBallToPivot()
        {
            if (CurrentBall is Component ballComp && currentSpawnPoint != null)
            {
                ballComp.transform.SetParent(currentSpawnPoint, true);
                ballComp.transform.localPosition = Vector3.zero;
                ballComp.transform.localRotation = Quaternion.identity;

                if (ballComp.TryGetComponent<Rigidbody>(out var ballRb))
                {
                    ballRb.isKinematic = true;
                    ballRb.velocity = Vector3.zero;
                    ballRb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void RefillColorQueue()
        {
            while (_colorDataQueue.Count < _baseBallBag.Count)
            {
                if (_currentBag.Count == 0)
                {
                    _currentBag = new List<BallColorType>(_baseBallBag);
                    ShuffleBag();
                }
                
                int lastIndex = _currentBag.Count - 1;
                _colorDataQueue.Enqueue(_currentBag[lastIndex]);
                _currentBag.RemoveAt(lastIndex);
            }
        }

        private void ShuffleBag()
        {
            int n = _currentBag.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (_currentBag[k], _currentBag[n]) = (_currentBag[n], _currentBag[k]);
            }
        }
    }
}