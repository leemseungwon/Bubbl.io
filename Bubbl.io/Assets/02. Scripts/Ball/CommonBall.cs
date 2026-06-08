using System;
using System.Collections;
using _02._Scripts.Core.Managers;
using _02._Scripts.Core.Systems;
using _02._Scripts.Entity;
using UnityEngine;

namespace _02._Scripts.Ball
{
    [RequireComponent(typeof(Rigidbody))]
    public class CommonBall : MonoBehaviour, IPoolable
    {
        [SerializeField] private float launchSpeed;
        [SerializeField] private int maxReflectCount = 3;
        [SerializeField] private float ballRadius = 0.5f;
        [SerializeField] private float destroyTimeByFalling = 1f;
        
        public BallColorType Color { get; set; }
        private float MaxReflectCount { get; set; }
        public event Action OnSnapToGrid;
        public bool IsSnapped {get; private set; } = false;
        public Entity.Entity Owner { get; private set; }
        
        private PoolingManager _poolingManager;
        private BallSystem _ballSystem;
        private MeshRenderer _renderer;
        private Rigidbody _rigidbody;

        private WaitForSeconds _destroyByFalling;
        private Vector3 _moveDirection;
        private Vector3 _lastVelocity;
        private bool _isLaunched = false;
        
        private const string BallTag = "Ball";
        private const string IgnoreBallTag = "IgnoreBall";
        private const string TopWallTag = "TopWall";
        private const string WallTag = "Wall";

        private void Start()
        {
            _destroyByFalling = new WaitForSeconds(destroyTimeByFalling);
        }

        public void Initialize()
        {
            _poolingManager = Manager.Instance.GetManager<PoolingManager>();
            _ballSystem = Core.Systems.System.Instance.GetSystem<BallSystem>();
            
            _renderer = GetComponent<MeshRenderer>();
            _rigidbody = GetComponent<Rigidbody>();
            
            int ownLayer = LayerMask.NameToLayer(BallTag);
            
            _isLaunched = false;
            _rigidbody.isKinematic = true; 
        }
        
        public void SpawnBall(Entity.Entity owner, BallColorType colorType)
        {
            Owner = owner;
            
            _isLaunched = false;
            _rigidbody.isKinematic = true; 
            _rigidbody.velocity = Vector3.zero;
            _moveDirection = Vector3.zero;
            
            _renderer.material = _ballSystem.GetMaterial(colorType);
            gameObject.layer = LayerMask.NameToLayer(BallTag);
            
            Color = colorType;
        }

        public void Launch(Vector2 direction)
        {
            MaxReflectCount = maxReflectCount;

            transform.SetParent(null, true);
            transform.rotation = Quaternion.identity;

            _rigidbody.isKinematic = true;

            _moveDirection = new Vector3(direction.x, direction.y, 0f).normalized;
    
            _isLaunched = true;
        }

        private void FixedUpdate()
        {
            if (!_isLaunched || IsSnapped) return;

            float step = launchSpeed * Time.fixedDeltaTime;
            RaycastHit hit;

            if (Physics.SphereCast(transform.position, ballRadius * 0.8f, _moveDirection, out hit, step))
            {
                if (hit.collider.CompareTag("Wall")) 
                {
                    _moveDirection = Vector3.Reflect(_moveDirection, hit.normal);
                    _moveDirection.z = 0;
                }
                else if (hit.collider.CompareTag("TopWall") || 
                         (hit.collider.TryGetComponent(out CommonBall ball) && ball.IsSnapped))
                {
                    StopAndSnap();
                    return;
                }
            }
            transform.position += _moveDirection * step;
        }

        private void StopAndSnap()
        {
            if (IsSnapped) return;
    
            IsSnapped = true;
            _isLaunched = false;
            _rigidbody.velocity = Vector3.zero;

            if (Owner != null && Owner.OwnGrid != null)
            {
                Owner.OwnGrid.SnapToGrid(this);
            }
            
            OnSnapToGrid?.Invoke();
        }
        
        public void SetFallingMode()
        {
            IsSnapped = false;
            _isLaunched = false;

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            gameObject.layer = LayerMask.NameToLayer(IgnoreBallTag);

            StartCoroutine(DestroyByFalling());
        }

        private IEnumerator DestroyByFalling()
        {
            yield return _destroyByFalling;
            Owner.OwnGrid.RemoveBall(this);
            DestroyBall();
        }
        
        public void DestroyBall()
        {
            transform.SetParent(null);
            transform.localPosition = Vector3.zero;
            _poolingManager.DespawnPool("Ball", this);
            IsSnapped = false;
            OnSnapToGrid?.Invoke();
        }

        public void Reset() 
        {
            _isLaunched = false;
            _rigidbody.useGravity = false;
            _moveDirection = Vector3.zero;
        }
    }
}