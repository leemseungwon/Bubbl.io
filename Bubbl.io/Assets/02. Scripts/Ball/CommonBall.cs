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
        
        public BallColorType Color { get; set; }

        private BallSystem _ballSystem;
        private MeshRenderer _renderer;
        private Rigidbody _rigidbody;
        private Collider _collider;
        
        public void Initialize()
        {
            _ballSystem = Core.Systems.System.Instance.GetSystem<BallSystem>();
            
            _renderer = GetComponent<MeshRenderer>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }
        
        public void SetColor(BallColorType colorType)
        {
            _renderer.materials[0] = _ballSystem.GetMaterial(colorType);
            Color = colorType;
        }

        public void Launch(Vector2 direction)
        {
            transform.SetParent(null, false);
            transform.rotation = Quaternion.identity;
            _rigidbody.isKinematic = false;

            Vector3 launchVelocity = new Vector3(direction.x, direction.y, 0f) * launchSpeed;
            _rigidbody.velocity = launchVelocity;

            _rigidbody.angularVelocity = Vector3.zero;
        }

        public void Reset() { }
    }
}