using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Sound Settings")]
        [SerializeField] private float walkSoundIntensity = 0.5f;
        [SerializeField] private float runSoundIntensity = 0.7f;
        [SerializeField] private float jumpSoundIntensity = 0.8f;
        [SerializeField] private float footstepInterval = 0.5f;
        [SerializeField] private bool emitSounds = true;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float footstepTimer = 0f;
        private Transform myTransform;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            myTransform = transform;
        }

        void Update()
        {
            HandleMovement();
            HandleGravity();
        }

        private void HandleMovement()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // isCrouching = Input.GetKey(KeyCode.LeftControl);
            // targetHeight = isCrouching ? originalHeight * crouchHeightMultiplier : originalHeight;

            // if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
            // {
            //     float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            //
            //     Vector3 center = controller.center;
            //     float heightDifference = newHeight - controller.height;
            //     center.y += heightDifference / 2f;
            //
            //     controller.height = newHeight;
            //     controller.center = center;
            // }

            // if (cameraController != null)
            // {
            //     float targetCrouchOffset = isCrouching ? crouchCameraOffset : 0f;
            //     currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, Time.deltaTime * crouchTransitionSpeed);
            //     cameraController.crouchOffset = currentCrouchOffset;
            // }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = myTransform.right * x + myTransform.forward * z;

            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? walkSpeed * runSpeedMultiplier : walkSpeed;

            controller.Move(move * currentSpeed * Time.deltaTime);

            if (emitSounds && move.sqrMagnitude > 0.01f && isGrounded)
            {
                HandleFootsteps(isRunning);
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (emitSounds) SoundManager.EmitSound(myTransform.position, jumpSoundIntensity, gameObject);
            }
        }

        private void HandleGravity()
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleFootsteps(bool isRunning)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                float intensity = isRunning ? runSoundIntensity : walkSoundIntensity;
                SoundManager.EmitSound(myTransform.position, intensity, gameObject);

                footstepTimer = isRunning ? footstepInterval * 0.7f : footstepInterval;
            }
        }
    }
}