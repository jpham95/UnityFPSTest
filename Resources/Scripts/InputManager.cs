using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace FPSController.Manager
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] public PlayerInput PlayerInput;

        public Vector2 Move {get; private set;}
        public Vector2 Look {get; private set;}
        public bool Sprint {get; private set;}
        public bool Jump {get; set;}
        public bool Crouch {get; set;}

        private InputActionMap _currentMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;

        private void Awake()
        {
            HideCursor();
            _currentMap = PlayerInput.currentActionMap;
            _moveAction = _currentMap.FindAction("Move");
            _lookAction = _currentMap.FindAction("Look");
            _sprintAction = _currentMap.FindAction("Sprint");
            _jumpAction = _currentMap.FindAction("Jump");
            _crouchAction = _currentMap.FindAction("Crouch");            

            _moveAction.performed += OnMove;
            _lookAction.performed += OnLook;
            _sprintAction.performed += OnSprint;
            _jumpAction.performed += OnJump;
            _crouchAction.performed += OnCrouch;

            _moveAction.canceled += OnMove;
            _lookAction.canceled += OnLook;
            _sprintAction.canceled += OnSprint;
            _jumpAction.canceled += OnJump;
            _crouchAction.canceled += OnCrouch;


        }

        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            Sprint = context.ReadValueAsButton();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            Jump = context.ReadValueAsButton();
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            Crouch = context.ReadValueAsButton();
        }
        private void OnEnable()
        {
            _currentMap.Enable();
        }
        
        private void OnDisable()
        {
            _currentMap.Disable();
        }
    }
}