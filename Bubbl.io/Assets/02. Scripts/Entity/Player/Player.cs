
using UnityEngine;

namespace _02._Scripts.Entity.Player
{
    public class Player : Entity
    {
        private Camera _mainCamera;
        private InputComponent _inputComponent;
        private Vector2 _currentMouseScreenPosition;
        
        private HandleBallComponent _handleBallComponent;
        
        protected override void Awake()
        {
            base.Awake();
            _mainCamera = Camera.main;
            
            _inputComponent = GetCompo<InputComponent>();
            _handleBallComponent = GetCompo<HandleBallComponent>();
            _inputComponent.OnMouseMovement += UpdateMousePosition;
            _inputComponent.OnLaunchBall += Launch;
        }

        private void UpdateMousePosition(Vector2 mousePosition)
        {
            _currentMouseScreenPosition = mousePosition;
        }
        
        private void Update()
        {
            RotateToMouse();
        }
        
        private void RotateToMouse()
        {
            Plane playerPlane = new Plane(Vector3.forward, transform.position);
            Ray ray = _mainCamera.ScreenPointToRay(_currentMouseScreenPosition);
            
            if (playerPlane.Raycast(ray, out float enterDistance))
            {
                Vector3 worldMousePosition = ray.GetPoint(enterDistance);
                Vector3 direction = worldMousePosition - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                
                transform.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }

        private void Launch()
        {
            _handleBallComponent.LaunchCurrentBall(transform.up);
        }

        private void OnDestroy()
        {
            if (_inputComponent != null)
            {
                _inputComponent.OnMouseMovement -= UpdateMousePosition;
                _inputComponent.OnLaunchBall -= Launch;
            }
        }
    }
}