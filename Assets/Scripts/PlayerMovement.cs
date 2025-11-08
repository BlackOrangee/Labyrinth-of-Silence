using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 5f;
        public float runSpeedMultiplier = 1.5f;
        public float gravity = -9.81f;
        public float jumpHeight = 1.5f;

        [Header("Sound Settings")]
        [Tooltip("Sound intensity when walking")]
        public float walkSoundIntensity = 0.5f;
        [Tooltip("Sound intensity when running (shift)")]
        public float runSoundIntensity = 0.7f;
        [Tooltip("Sound intensity when jumping")]
        public float jumpSoundIntensity = 0.8f;
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

        void Start()
        {
            controller = GetComponent<CharacterController>();
            runSpeed = speed * runSpeedMultiplier;
            jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        void Update()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            bool isMoving = move.sqrMagnitude > MovementThresholdSqr;

            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : speed;

            controller.Move(move * currentSpeed * Time.deltaTime);

            if (emitSounds && isMoving && isGrounded)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    float soundIntensity = isRunning ? runSoundIntensity : walkSoundIntensity;
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
