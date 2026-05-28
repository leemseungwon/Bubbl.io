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
        Yellow,
        Black,
        White,
        Purple
    }
    
    public class HandleBallComponent : MonoBehaviour, IEntityComponent
    {
        [Header("7-Bag Settings")]
        [SerializeField] private List<BallColorType> baseBallBag = new List<BallColorType> 
        { 
            BallColorType.Red, BallColorType.Blue, BallColorType.Green, BallColorType.Yellow, BallColorType.Black, 
            BallColorType.White, BallColorType.Purple
        };

        [Header("Positions")]
        [SerializeField] private Transform ballSpawnPoint;
        [SerializeField] private Transform currentSpawnPoint; 
        [SerializeField] private Transform nextSpawnPoint;

        private List<BallColorType> _currentBag = new List<BallColorType>();
        private Queue<BallColorType> _colorDataQueue = new Queue<BallColorType>();
        private BallSystem _ballSystem;
        private Entity _owner;
        
        public IBall CurrentBall { get; private set; }
        public IBall NextBall { get; private set; }

        public void Initialize(Entity entity)
        {
            _owner = entity;
            _ballSystem = Core.Systems.System.Instance.GetSystem<BallSystem>(); 
        }
        
        public void Start()
        {
            RefillColorQueue();
            
            SetupInitialPositions();
        }
        
        private void SetupInitialPositions()
        {
            CurrentBall = SpawnBallFromQueue();
            if(CurrentBall != null)
                AttachBallToPivot(CurrentBall, currentSpawnPoint);
            
            NextBall = SpawnBallFromQueue();
            if(NextBall != null)
                AttachBallToPivot(NextBall, nextSpawnPoint);
        }
        
        public void LaunchCurrentBall(Vector2 launchDirection)
        {
            if (CurrentBall == null) 
                return;
            
            CurrentBall.Launch(launchDirection);
            
            CurrentBall = NextBall;
            NextBall = OutputBall();
        }
        
        private IBall SpawnBallFromQueue()
        {
            if (_colorDataQueue.Count == 0)
            {
                RefillColorQueue();
            }

            BallColorType nextColor = _colorDataQueue.Dequeue();
            RefillColorQueue();

            return _ballSystem?.CreateBall(nextColor, currentSpawnPoint);
        }
        
        private void AttachBallToPivot(IBall ball, Transform targetPivot)
        {
            if (ball is Component ballComp && targetPivot != null)
            {
                ballComp.transform.SetParent(targetPivot, false);
                ballComp.transform.localPosition = Vector3.zero;
                ballComp.transform.localRotation = Quaternion.identity;

                if (ballComp.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = true;
                }
            }
        }

        protected IBall OutputBall()
        {
            if (_colorDataQueue.Count == 0) 
                RefillColorQueue();

            BallColorType nextColor = _colorDataQueue.Dequeue();
            RefillColorQueue();
            
            return _ballSystem.CreateBall(nextColor, ballSpawnPoint);
        }

        protected virtual void RefillColorQueue()
        {
            while (_colorDataQueue.Count < baseBallBag.Count)
            {
                if (_currentBag.Count == 0)
                {
                    _currentBag = new List<BallColorType>(baseBallBag);
                    ShuffleBall();
                }
                int lastIndex = _currentBag.Count - 1;
                _colorDataQueue.Enqueue(_currentBag[lastIndex]);
                _currentBag.RemoveAt(lastIndex);
            }
        }

        protected virtual void ShuffleBall()
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