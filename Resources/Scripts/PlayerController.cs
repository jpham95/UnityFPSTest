using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FPSController.Manager;

namespace FPSController.PlayerControl
{    
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float lerpSpeed = 5.0f;
        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float LowerLimit = 70f;
        [SerializeField] private float MouseSensitivity = 21.9f;
        [SerializeField] public float JumpHeight = 1.2f;
        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private Animator _animator;
        private CapsuleCollider _colliderCapsule;
        private bool _hasAnimator;
        private int _xVelHash;
        private int _yVelHash;
        private int _crouchHash;
		private float _speed;
        private float _xRotation;
        private float _xRotationSpeed;
        private float FallTimeout = 0.15f;
        private float JumpTimeout = 0.1f;
        public bool Grounded = true;
		public float GroundedOffset = -0.14f;
		public float GroundedRadius = 0.5f;
		public float Gravity = -15.0f;
        private bool _isCrouching = false;
		public LayerMask GroundLayers;

        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _crouchSpeed = 2f;
        [SerializeField] private float _runSpeed = 6f;
        private float targetSpeed;
        private Vector2 _currentVelocity;
        private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

        // Start is called before the first frame update
        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();
            _colliderCapsule = GetComponent<CapsuleCollider>();

            _xVelHash = Animator.StringToHash("VelocityX");
            _yVelHash = Animator.StringToHash("VelocityY");
            _crouchHash = Animator.StringToHash("isCrouching");
        }

        private void FixedUpdate()
        {
            if (_playerRigidbody.position.y < -10f) _playerRigidbody.position = Vector3.zero;
            Crouch();
            JumpAndGravity();
			GroundedCheck();
            Move();
            
        }
        private void LateUpdate()
        {
            CamMovements();
        }
        private bool IsCurrentDeviceMouse
        {
            get
            {
                #if ENABLE_INPUT_SYSTEM
                return _inputManager.PlayerInput.currentControlScheme == "KeyboardMouse";
                #else
                return false;
                #endif
            }
        }
        private void Move()
        {
            if (_isCrouching)
            {
                // set target speed to crouch speed
                targetSpeed = _crouchSpeed;
            }
            else
            {
                // set target speed based on move speed, sprint speed and if sprint is pressed
                targetSpeed = _inputManager.Sprint ? _runSpeed : _walkSpeed;
            }

            // if there is no input, set the target speed to 0
            if (_inputManager.Move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            // magnitude is used to determine total speed, we just want the x,z components
            float currentHorizontalSpeed = new Vector3(_playerRigidbody.velocity.x, 0.0f, _playerRigidbody.velocity.z).magnitude;

            float speedOffset = 0.1f;

            // accelerate or decelerate to target speed
            // if current speed is not approximately equal to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * lerpSpeed);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // normalise input direction
            Vector3 inputDirection = new Vector3(_inputManager.Move.x, 0.0f, _inputManager.Move.y).normalized;

            // if there is a move input rotate player when the player is moving
            if (_inputManager.Move != Vector2.zero)
            {
                inputDirection = transform.right * _inputManager.Move.x + transform.forward * _inputManager.Move.y;
            }

            // calculate the velocity difference
            Vector3 targetVelocity = inputDirection * _speed;
            Vector3 velocityDiff = targetVelocity - _playerRigidbody.velocity;

            // apply the force to the player rigidbody
            _playerRigidbody.AddForce(velocityDiff, ForceMode.VelocityChange);

            float currentXVel = _animator.GetFloat(_xVelHash);
            float newXVel = Mathf.Lerp(currentXVel, _playerRigidbody.velocity.x, Time.deltaTime * lerpSpeed);
            _animator.SetFloat(_xVelHash, newXVel);
            
            float currentYVel = _animator.GetFloat(_yVelHash);
            float newYVel = Mathf.Lerp(currentYVel, _playerRigidbody.velocity.z, Time.deltaTime * lerpSpeed);
            _animator.SetFloat(_yVelHash, newYVel);
        }
        private void Crouch()
        {
            // Toggle crouch state
            _isCrouching = _inputManager.Crouch ? !_isCrouching : _isCrouching;

            // Update the crouch animation state
            _animator.SetBool(_crouchHash, _isCrouching);

            if (_isCrouching)
            {
                // Set the collider height and radius based on the crouch state
                _colliderCapsule.height = 1.3f;
                _colliderCapsule.radius = 0.5f;
                _colliderCapsule.center = new Vector3(0, 0.65f, 0);
            }
            else
            {
                // Set the collider height and radius based on the crouch state
                _colliderCapsule.height = 1.78f;
                _colliderCapsule.radius = 0.28f;
                _colliderCapsule.center = new Vector3(0, 0.89f, 0);
            }
            Debug.Log($"isCrouching: {_isCrouching}");
        }
		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
        private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_inputManager.Jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_inputManager.Jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

        private void CamMovements()
        {
            var MouseX = _inputManager.Look.x;
            var MouseY = _inputManager.Look.y;
            Camera.position = CameraRoot.position;

            _xRotation -= MouseY * MouseSensitivity * Time.deltaTime;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, LowerLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);

            transform.Rotate(Vector3.up * MouseX * MouseSensitivity * Time.deltaTime);
            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(Vector3.up * MouseX * MouseSensitivity * Time.smoothDeltaTime));
        }
    

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}
    }
}