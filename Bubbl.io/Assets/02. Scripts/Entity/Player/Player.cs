
using UnityEngine;

namespace _02._Scripts.Entity.Player
{
    public class Player : Entity
    {
        private Camera _mainCamera;
        private InputComponent _inputComponent;
        private Vector2 _currentMouseScreenPosition;
        
        private HandleBallComponent _handleBallComponent;
        private Ball.CommonBall _launchedBallWaitingForSnap = null;
        
        protected override void Awake()
        {
            base.Awake();
            _mainCamera = Camera.main;
            
            _inputComponent = GetCompo<InputComponent>();
            _handleBallComponent = GetCompo<HandleBallComponent>();
            _inputComponent.OnMouseMovement += UpdateMousePosition;
            _inputComponent.OnLaunchBall += Launch;
        }

        private void Start()
        {
            // GridBall의 팝 이벤트 구독
            if (OwnGrid != null)
            {
                OwnGrid.OnBallsPopped += OnBallsPopped;
            }
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
            if (_handleBallComponent != null && _handleBallComponent.CurrentBall != null)
            {
                _handleBallComponent.LaunchCurrentBall(transform.up);
                
                // 발사한 공을 추적
                _launchedBallWaitingForSnap = _handleBallComponent.CurrentBall;
                if (_launchedBallWaitingForSnap != null)
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid += OnLaunchedBallSnapped;
                }
            }
        }

        private void OnLaunchedBallSnapped()
        {
            // 공이 스냅되면: 구독 해제, 상태 정리
            if (_launchedBallWaitingForSnap != null)
            {
                try
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                }
                catch { }
            }
            _launchedBallWaitingForSnap = null;
        }

        private void OnBallsPopped(System.Collections.Generic.List<Ball.CommonBall> poppedBalls)
        {
            // 발사한 공이 팝되었으면 상태 초기화
            if (_launchedBallWaitingForSnap != null && poppedBalls.Contains(_launchedBallWaitingForSnap))
            {
                _launchedBallWaitingForSnap = null;
            }
        }

        private void OnDestroy()
        {
            // InputComponent 구독 해제
            if (_inputComponent != null)
            {
                _inputComponent.OnMouseMovement -= UpdateMousePosition;
                _inputComponent.OnLaunchBall -= Launch;
            }

            // GridBall 팝 이벤트 구독 해제
            if (OwnGrid != null)
            {
                try
                {
                    OwnGrid.OnBallsPopped -= OnBallsPopped;
                }
                catch { }
            }

            // 발사 공 구독 해제
            if (_launchedBallWaitingForSnap != null)
            {
                try
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                }
                catch { }
            }
        }
    }
}