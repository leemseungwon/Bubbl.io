using System;
using System.Collections;
using _02._Scripts.Core.Managers;
using _02._Scripts.Core.Systems;
using _02._Scripts.Entity;
using UnityEngine;

namespace _02._Scripts.Ball
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
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
        private Collider _collider;

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
            _collider = GetComponent<Collider>();
            
            _isLaunched = false;
            _rigidbody.isKinematic = true; 
            _collider.isTrigger = true;
        }
        
        public void SpawnBall(Entity.Entity owner, BallColorType colorType)
        {
            Owner = owner;
            _collider.enabled = true;
            
            _isLaunched = false;
            _rigidbody.isKinematic = true; 
            _collider.isTrigger = true;
            _rigidbody.velocity = Vector3.zero;
            _moveDirection = Vector3.zero;
            
            _renderer.material = _ballSystem.GetMaterial(colorType);
            gameObject.layer = LayerMask.NameToLayer(BallTag);
            
            Color = colorType;
        }

        public void Launch(Vector2 direction)
        {
            MaxReflectCount = maxReflectCount;
            
            transform.SetParent(null, false);
            transform.rotation = Quaternion.identity;

            _rigidbody.isKinematic = true;

            _moveDirection = new Vector3(direction.x, direction.y, 0f).normalized;
            
            _isLaunched = true;
        }

        private void Update()
        {
            if (!_isLaunched || IsSnapped) 
                return;

            float moveDistance = launchSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;

            if (Physics.SphereCast(currentPos, ballRadius * 0.95f, _moveDirection, out RaycastHit hit, moveDistance))
            {
                if (MaxReflectCount <= 0)
                {
                    DestroyBall();
                    return;
                }
                
                if (hit.collider.CompareTag(WallTag))
                {
                    MaxReflectCount--;
                    transform.position = currentPos + (_moveDirection * hit.distance);

                    Vector3 normal = hit.normal;
                    normal.z = 0f;
                    _moveDirection = Vector3.Reflect(_moveDirection, normal.normalized).normalized;
                }
                else if (hit.collider.CompareTag(BallTag) || hit.collider.CompareTag(TopWallTag))
                {
                    Vector3 hitPos = currentPos + (_moveDirection * hit.distance);
                    Vector3 correctedPos = hitPos + (_moveDirection * (ballRadius * 0.5f));

                    correctedPos.z = Owner.OwnGrid.transform.position.z;
                    transform.position = correctedPos;

                    StopAndSnap();
                }
            }
            else
            {
                transform.position += _moveDirection * moveDistance;
            }
        }

        private void StopAndSnap()
        {
            IsSnapped = true;
            _isLaunched = false;

            if (Owner != null && Owner.OwnGrid != null)
            {
                Vector3 currentPos = transform.position;
                currentPos.z = Owner.OwnGrid.transform.position.z;
                transform.position = currentPos;
                
                Owner.OwnGrid.SnapToGrid(this);
            }

            OnSnapToGrid?.Invoke();
        }
        
        public void SetFallingMode()
        {
            IsSnapped = false;
            _isLaunched = false;
            
            _collider.enabled = false;

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            gameObject.layer = LayerMask.NameToLayer(IgnoreBallTag);

            StartCoroutine(DestroyByFalling());
        }

        private IEnumerator DestroyByFalling()
        {
            yield return _destroyByFalling;
            DestroyBall();
            Owner.OwnGrid.RemoveBall(this);
        }
        
        public void DestroyBall()
        {
            transform.SetParent(null);
            transform.localPosition = Vector3.zero;
            _poolingManager.DespawnPool("Ball", this);
            IsSnapped = false;
        }

        public void Reset() 
        {
            _isLaunched = false;
            _rigidbody.useGravity = false;
            _moveDirection = Vector3.zero;
        }
    }
}