
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _02._Scripts.Entity.Player
{
    public class InputComponent : MonoBehaviour, PlayerInput.IPlayerActions, IEntityComponent
    {
        public event Action<Vector2> OnMouseMovement; 
        public event Action OnLaunchBall; 
        
        private PlayerInput _playerInput;
        
        public void Initialize(Entity entity)
        {
            if (_playerInput == null)
            {
                _playerInput = new PlayerInput();
                _playerInput.Player.SetCallbacks(this);
            }
            _playerInput.Enable();
        }

        public void OnMouse(InputAction.CallbackContext context)
        {
            OnMouseMovement?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLaunch(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnLaunchBall?.Invoke();
        }

        private void OnDisable()
        {
            _playerInput?.Disable();
        }

        private void OnDestroy()
        {
            _playerInput?.Dispose();
        }
    }
}