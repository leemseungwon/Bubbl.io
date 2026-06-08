
using System.Collections;
using System.Collections.Generic;
using _02._Scripts.Ball;
using _02._Scripts.Core.Systems;
using UnityEngine;
using Random = UnityEngine.Random;

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
        [SerializeField] private Transform currentSpawnPoint;
        [SerializeField] private float launchBallTime = 0.5f;

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
        private float _currentLaunchTime = 0f;
        private CommonBall _lastLaunchedBall;
        private CommonBall _activeLaunchedBall;
        private float _safetyTimeout = 3.0f; // 3초 동안 스냅 안 되면 강제 초기화
        private float _launchTimer = 0f;
        
        public bool IsEnd { get; set; }
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

        private void Update()
        {
            _currentLaunchTime -= Time.deltaTime;
            
            if (!_canLaunchBall && _currentLaunchTime <= 0f)
            {
                _launchTimer += Time.deltaTime;
                if (_launchTimer > _safetyTimeout)
                {
                    ForceResetLaunchState();
                }
            }
        }

        private void ForceResetLaunchState()
        {
            if (CurrentBall != null) 
                CurrentBall.OnSnapToGrid -= SnapBallComplete;
    
            _owner.OwnGrid.IsAutoLowering = true;
            _canLaunchBall = true; // 강제 복구
            _launchTimer = 0f;
            
            if (CurrentBall == null) 
                SpawnNextBall();
        }

        public void LaunchCurrentBall(Vector2 launchDirection)
        {
            if (IsEnd || CurrentBall == null || !_canLaunchBall) return;

            _canLaunchBall = false;
            _owner.OwnGrid.IsAutoLowering = false; // 발사 중 격자 이동 금지
    
            _activeLaunchedBall = CurrentBall;
            CurrentBall = null;

            _activeLaunchedBall.OnSnapToGrid += SnapBallComplete;
            _activeLaunchedBall.Launch(launchDirection);
        }

        private void SnapBallComplete()
        {
            if (_activeLaunchedBall != null)
            {
                _activeLaunchedBall.OnSnapToGrid -= SnapBallComplete;
                _activeLaunchedBall = null;
            }
    
            ForceResetLaunchState();
            SpawnNextBall();
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
        
        public void Restart()
        {
            StopAllCoroutines();
    
            // 이전 발사체 정리
            if (_activeLaunchedBall != null) { 
                _activeLaunchedBall.OnSnapToGrid -= SnapBallComplete; 
                _activeLaunchedBall.DestroyBall(); 
                _activeLaunchedBall = null; 
            }
            if (CurrentBall != null) { CurrentBall.DestroyBall(); CurrentBall = null; }

            // 상태 변수 초기화
            _canLaunchBall = true;
            _owner.OwnGrid.IsAutoLowering = true;
            _launchTimer = 0f;
            IsEnd = false;
    
            SpawnNextBall();
        }
    }
}