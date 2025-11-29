using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 5f;
        public float runSpeedMultiplier = 1.5f;
        public float gravity = -9.81f;
        public float jumpHeight = 0f;

        [Header("Crouch Settings")]
        [Tooltip("Speed multiplier when crouching")]
        public float crouchSpeedMultiplier = 0.5f;
        [Tooltip("Height multiplier when crouching")]
        public float crouchHeightMultiplier = 0.5f;
        [Tooltip("Crouch transition speed")]
        public float crouchTransitionSpeed = 8f;
        [Tooltip("Camera height offset when crouching")]
        public float crouchCameraOffset = -0.5f;

        [Header("Sound Settings")]
        [Tooltip("Sound intensity when walking")]
        public float walkSoundIntensity = 0.5f;
        [Tooltip("Sound intensity when running (shift)")]
        public float runSoundIntensity = 0.7f;
        [Tooltip("Sound intensity when crouching")]
        public float crouchSoundIntensity = 0.2f;
        [Tooltip("Sound intensity when jumping")]
        public float jumpSoundIntensity = 0f;
        [Tooltip("Interval between footstep sounds (seconds)")]
        public float footstepInterval = 0.5f;
        [Tooltip("Generate sounds")]
        public bool emitSounds = true;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float footstepTimer = 0f;

        private const float MovementThresholdSqr = 0.01f;
        private float runSpeed;
        private float jumpVelocity;

        private float originalHeight;
        private float targetHeight;
        private bool isCrouching;

        private CameraController cameraController;
        private float currentCrouchOffset;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            runSpeed = speed * runSpeedMultiplier;
            jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            originalHeight = controller.height;
            targetHeight = originalHeight;

            cameraController = FindObjectOfType<CameraController>();
            currentCrouchOffset = 0f;
        }

        void Update()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            isCrouching = Input.GetKey(KeyCode.LeftControl);
            targetHeight = isCrouching ? originalHeight * crouchHeightMultiplier : originalHeight;

            if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
            {
                float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

                Vector3 center = controller.center;
                float heightDifference = newHeight - controller.height;
                center.y += heightDifference / 2f;

                controller.height = newHeight;
                controller.center = center;
            }

            if (cameraController != null)
            {
                float targetCrouchOffset = isCrouching ? crouchCameraOffset : 0f;
                currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, Time.deltaTime * crouchTransitionSpeed);
                cameraController.crouchOffset = currentCrouchOffset;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            bool isMoving = move.sqrMagnitude > MovementThresholdSqr;

            bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
            float currentSpeed = speed;
            if (isCrouching)
            {
                currentSpeed *= crouchSpeedMultiplier;
            }
            else if (isRunning)
            {
                currentSpeed = runSpeed;
            }

            controller.Move(move * currentSpeed * Time.deltaTime);

            if (emitSounds && isMoving && isGrounded)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    float soundIntensity = walkSoundIntensity;
                    if (isCrouching)
                    {
                        soundIntensity = crouchSoundIntensity;
                    }
                    else if (isRunning)
                    {
                        soundIntensity = runSoundIntensity;
                    }

                    SoundManager.EmitSound(transform.position, soundIntensity, gameObject);
                    footstepTimer = footstepInterval;
                }
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = jumpVelocity;

                if (emitSounds)
                {
                    SoundManager.EmitSound(transform.position, jumpSoundIntensity, gameObject);
                }
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}