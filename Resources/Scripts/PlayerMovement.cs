using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FPSController;
/*
TODO:
> Add jump
> Add prone
> Add freelook
> Add lean
> Turning animations
> Aim down sights
*/
namespace FPSController.PlayerMovement
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Transform playerCamera;
        [SerializeField] private Transform playerCameraRoot;
        [SerializeField] private float _mouseSensitivity = 21.9f;
        private CustomInput input = null;
        private Vector2 moveInput = Vector2.zero;
        private Rigidbody _playerRigidbody;
        private Animator _animator;
        private CapsuleCollider _colliderCapsule;
        private float lerpSpeed = 5.0f;
        private float _targetSpeed;
        private float _currentSpeed;
        private int _xVelHash;
        private int _yVelHash;
        private int _crouchHash;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        private bool _isFreelooking = false;
        private float _defaultSpeed = 4f;
        private float _sprintSpeed = 6f;
        private float _crouchSpeed = 2f;
        private float _lookRotationTarget = 0f;
        private float _forceVelocity;


        private void Awake()
        {
            HideCursor();

            input = new CustomInput(); // refactor this to use the input manager

            _playerRigidbody = GetComponent<Rigidbody>();
            _colliderCapsule = GetComponent<CapsuleCollider>();
            _animator = GetComponent<Animator>();

            _xVelHash = Animator.StringToHash("VelocityX");
            _yVelHash = Animator.StringToHash("VelocityY");
            _crouchHash = Animator.StringToHash("isCrouching");
        }
        // Start is called before the first frame update
        private void OnEnable()
        {
            input.Enable();
            input.Player.Movement.performed += OnMovementPerformed;
            input.Player.Movement.canceled += OnMovementCanceled;
            input.Player.Sprint.performed += OnSprintPerformed;
            input.Player.Sprint.canceled += OnSprintCanceled;
            input.Player.Crouch.performed += OnCrouchPerformed;
            input.Player.Crouch.canceled += OnCrouchCanceled;
            input.Player.Freelook.performed += OnLookPerformed;
            input.Player.Freelook.canceled += OnLookCanceled;
        }

        // Update is called once per frame
        private void OnDisable()
        {
            input.Disable();
            input.Player.Movement.performed -= OnMovementPerformed;
            input.Player.Movement.canceled -= OnMovementCanceled;
            input.Player.Sprint.performed -= OnSprintPerformed;
            input.Player.Sprint.canceled -= OnSprintCanceled;
            input.Player.Crouch.performed -= OnCrouchPerformed;
            input.Player.Crouch.canceled -= OnCrouchCanceled;
            input.Player.Freelook.performed -= OnLookPerformed;
            input.Player.Freelook.canceled -= OnLookCanceled;
        }

        private void FixedUpdate()
        {   
            Crouch();
            Move();
            Debug.Log($"Freelooking: {_isFreelooking}");
        }

        private void LateUpdate()
        {
            CameraMovements();
        }
        private void OnMovementPerformed(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnMovementCanceled(InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _isSprinting = context.ReadValueAsButton();
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _isSprinting = false;
        }
        
        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            _isCrouching = !_isCrouching;
        }

        private void OnCrouchCanceled(InputAction.CallbackContext context)
        {
            // empty for now
            // use to implement hold to crouch
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            _isFreelooking = context.ReadValueAsButton();
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            _isFreelooking = false;
        }
        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Move()
        {
            if (_isSprinting)
            {
                // Cancel crouch
                _isCrouching = false;

                // set target speed to sprint speed
                _targetSpeed = _sprintSpeed;
            }
            else
            {
                // set target speed based on move speed, sprint speed and if sprint is pressed
                _targetSpeed = _isCrouching ? _crouchSpeed : _defaultSpeed;
            }

            //  if moveInput = Vector2.zero targetSpeed = 0.0f
            if (moveInput == Vector2.zero) _targetSpeed = 0.0f;
            // lerp between current speed and target speed
            _currentSpeed = new Vector3(_playerRigidbody.velocity.x, 0, _playerRigidbody.velocity.z).magnitude; 

            float speedOffset = 0.1f;

            if (_currentSpeed < _targetSpeed - speedOffset || _currentSpeed > _targetSpeed + speedOffset)
            {
                // create a new velocity with the target speed
                _forceVelocity = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * lerpSpeed);
                _forceVelocity = Mathf.Round(_forceVelocity * 1000f) / 1000f;
            }
            else
            {
                // set velocity to targetVelocity
                _forceVelocity = _targetSpeed;
            }

            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                inputDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            }

            // _forceVelocity *= inputDirection.magnitude;
            Vector3 targetVelocity = inputDirection * _forceVelocity;
            Vector3 velocityDiff = targetVelocity - _playerRigidbody.velocity;

            _playerRigidbody.AddForce(velocityDiff, ForceMode.VelocityChange);

            UpdateMovementAnimation();
        }

        private void Crouch()
        {
            if (_isCrouching)
            {
                // Set the collider height and radius based on the crouch state
                _colliderCapsule.height = Mathf.Lerp(_colliderCapsule.height, 1.3f, Time.deltaTime * lerpSpeed);
                _colliderCapsule.radius = Mathf.Lerp(_colliderCapsule.radius, 0.5f, Time.deltaTime * lerpSpeed);
                _colliderCapsule.center = Vector3.Lerp(_colliderCapsule.center, new Vector3(0, 0.65f, 0), Time.deltaTime * lerpSpeed);
            }
            else
            {
                // Set the collider height and radius based on the crouch state
                _colliderCapsule.height = Mathf.Lerp(_colliderCapsule.height, 1.78f, Time.deltaTime * lerpSpeed);
                _colliderCapsule.radius = Mathf.Lerp(_colliderCapsule.radius, 0.28f, Time.deltaTime * lerpSpeed);
                _colliderCapsule.center = Vector3.Lerp(_colliderCapsule.center, new Vector3(0, 0.89f, 0), Time.deltaTime * lerpSpeed);
            }
            
            _animator.SetBool(_crouchHash, _isCrouching);
        }

        private void CameraMovements()
        {
            Vector2 lookVector = input.Player.Look.ReadValue<Vector2>();

            var MouseX = lookVector.x;
            var MouseY = lookVector.y;
            playerCamera.position = playerCameraRoot.position;

            _lookRotationTarget -= MouseY * _mouseSensitivity * Time.deltaTime;
            // stop player breaking their neck
            _lookRotationTarget = Mathf.Clamp(_lookRotationTarget, -90f, 90f);

            playerCamera.localRotation = Quaternion.Euler(_lookRotationTarget, 0f, 0f);

            transform.Rotate(Vector3.up * MouseX * _mouseSensitivity * Time.deltaTime);
            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(Vector3.up * MouseX * _mouseSensitivity * Time.smoothDeltaTime));
        }

        private void UpdateMovementAnimation()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_playerRigidbody.velocity);

            float currentAnimatorX = _animator.GetFloat(_xVelHash);
            float newAnimatorX = Mathf.Lerp(currentAnimatorX, 1.8f*localVelocity.x, Time.deltaTime * lerpSpeed);
            _animator.SetFloat(_xVelHash, newAnimatorX);

            float currentAnimatorY = _animator.GetFloat(_yVelHash);
            float newAnimatorY = Mathf.Lerp(currentAnimatorY, 1.8f*localVelocity.z, Time.deltaTime * lerpSpeed);
            _animator.SetFloat(_yVelHash, newAnimatorY);
        }
    }
}