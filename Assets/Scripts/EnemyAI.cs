using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts
{
    /// <summary>
    /// Enemy AI with vision, hearing and chase systems
    /// Uses NavMesh for movement with obstacle avoidance
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Vision Settings")]
        [Tooltip("Vision range")]
        public float visionRange = 15f;
        [Tooltip("Field of view angle (in degrees)")]
        public float fieldOfViewAngle = 110f;
        [Tooltip("Player layer for raycast")]
        public LayerMask playerLayer;
        [Tooltip("Obstacle layer for line of sight check")]
        public LayerMask obstacleLayer;

        [Header("Hearing Settings")]
        [Tooltip("Hearing range")]
        public float hearingRange = 20f;

        [Header("Chase Settings")]
        [Tooltip("Attack distance")]
        public float attackRange = 2f;
        [Tooltip("Chase speed")]
        public float chaseSpeed = 5f;
        [Tooltip("Patrol speed")]
        public float patrolSpeed = 2.5f;
        [Tooltip("Time before losing target (seconds)")]
        public float loseTargetTime = 5f;

        [Header("Patrol Settings")]
        [Tooltip("Patrol points")]
        public Transform[] patrolPoints;
        [Tooltip("Wait time at each point")]
        public float waitTimeAtPoint = 2f;
        
        [Header("Debug")]
        public bool showDebugGizmos = true;
        
        public enum EnemyState
        {
            Patrol,
            Alert,
            Chase,
            Attack
        }
        
        private EnemyState currentState = EnemyState.Patrol;
        private NavMeshAgent navAgent;
        private int currentPatrolIndex = 0;
        private float waitTimer = 3f;
        private float loseTargetTimer = 0f;
        private Vector3 lastKnownPlayerPosition;
        private bool playerInSight = false;

        private float visionRangeSqr;
        private float attackRangeSqr;
        private float hearingRangeSqr;
        private float halfFieldOfView;
        private Vector3 eyeOffset;

        void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();

            if (navAgent == null)
            {
                Debug.LogError("NavMeshAgent not found! Add NavMeshAgent component to the enemy.");
                enabled = false;
                return;
            }

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("Player not found! Set 'Player' tag on the player object.");
                }
            }

            SoundManager.OnSoundEmitted += OnSoundHeard;

            navAgent.speed = patrolSpeed;
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                GoToNextPatrolPoint();
            }

            visionRangeSqr = visionRange * visionRange;
            attackRangeSqr = attackRange * attackRange;
            hearingRangeSqr = hearingRange * hearingRange;
            halfFieldOfView = fieldOfViewAngle * 0.5f;
            eyeOffset = Vector3.up;
        }
        
        void OnDestroy()
        {
            SoundManager.OnSoundEmitted -= OnSoundHeard;
        }
        
        void Update()
        {
            if (!player)
            {
                return;
            }
            
            CheckVision();
            
            switch (currentState)
            {
                case EnemyState.Patrol:
                    PatrolBehavior();
                    break;
                case EnemyState.Alert:
                    AlertBehavior();
                    break;
                case EnemyState.Chase:
                    ChaseBehavior();
                    break;
                case EnemyState.Attack:
                    AttackBehavior();
                    break;
            }
        }
        
        #region Vision System
        
        void CheckVision()
        {
            if (player == null)
            {
                return;
            }

            Vector3 eyePosition = transform.position + eyeOffset;
            Vector3 directionToPlayer = player.position - eyePosition;

            float distanceSqr = directionToPlayer.sqrMagnitude;

            if (distanceSqr > visionRangeSqr)
            {
                playerInSight = false;
                return;
            }

            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > halfFieldOfView)
            {
                playerInSight = false;
                return;
            }

            float distanceToPlayer = Mathf.Sqrt(distanceSqr);
            Vector3 directionNormalized = directionToPlayer / distanceToPlayer;

            RaycastHit hit;
            if (Physics.Raycast(eyePosition, directionNormalized, out hit, distanceToPlayer, playerLayer | obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                if (showDebugGizmos)
                {
                    Debug.DrawRay(eyePosition, directionNormalized * hit.distance, Color.red, 0.1f);
                }

                if (hit.transform == player || hit.transform.IsChildOf(player))
                {
                    playerInSight = true;
                    OnPlayerSpotted();

                    if (showDebugGizmos)
                    {
                        Debug.Log($"Enemy sees player! Distance: {distanceToPlayer:F2}, Angle: {angle:F2}");
                    }
                    return;
                }
                else
                {
                    if (showDebugGizmos)
                    {
                        Debug.Log($"Vision blocked by: {hit.transform.name}");
                    }
                }
            }
            else
            {
                if (showDebugGizmos)
                {
                    Debug.DrawRay(eyePosition, directionNormalized * visionRange, Color.yellow, 0.1f);
                    Debug.LogWarning("Raycast didn't hit anything! Check Player Layer and Obstacle Layer settings.");
                }
            }

            playerInSight = false;
        }
        
        void OnPlayerSpotted()
        {
            lastKnownPlayerPosition = player.position;
            loseTargetTimer = 0f;
            
            if (currentState == EnemyState.Patrol || currentState == EnemyState.Alert)
            {
                Debug.Log($"[EnemyAI] Player spotted! Changing from {currentState} to Chase");
                ChangeState(EnemyState.Chase);
            }
        }
        
        #endregion
        
        #region Hearing System
        
        void OnSoundHeard(Vector3 soundPosition, float soundIntensity, GameObject source)
        {
            if (source == gameObject)
            {
                return;
            }

            float distanceSqr = (soundPosition - transform.position).sqrMagnitude;
            float hearingRangeWithIntensity = hearingRange * soundIntensity;
            float hearingRangeWithIntensitySqr = hearingRangeWithIntensity * hearingRangeWithIntensity;

            if (distanceSqr <= hearingRangeWithIntensitySqr)
            {
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Alert)
                {
                    lastKnownPlayerPosition = soundPosition;
                    ChangeState(EnemyState.Alert);
                }
            }
        }
        
        #endregion
        
        #region State Behaviors
        
        void PatrolBehavior()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }
            
            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
                return;
            }
            
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
            {
                waitTimer = waitTimeAtPoint;
                GoToNextPatrolPoint();
            }
        }
        
        void AlertBehavior()
        {
            navAgent.speed = chaseSpeed;
            navAgent.SetDestination(lastKnownPlayerPosition);
            
            if (!navAgent.pathPending && navAgent.remainingDistance < 1f)
            {
                ChangeState(EnemyState.Patrol);
            }
        }
        
        void ChaseBehavior()
        {
            navAgent.speed = chaseSpeed;

            if (playerInSight)
            {
                navAgent.SetDestination(player.position);
                lastKnownPlayerPosition = player.position;
                loseTargetTimer = 0f;

                float distanceSqr = (player.position - transform.position).sqrMagnitude;
                if (distanceSqr <= attackRangeSqr)
                {
                    ChangeState(EnemyState.Attack);
                }
            }
            else
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
                loseTargetTimer += Time.deltaTime;

                if (loseTargetTimer >= loseTargetTime || (!navAgent.pathPending && navAgent.remainingDistance < 1f))
                {
                    ChangeState(EnemyState.Patrol);
                }
            }
        }
        
        void AttackBehavior()
        {
            navAgent.SetDestination(transform.position);

            Vector3 directionToPlayer = player.position - transform.position;

            Vector3 flatDirection = new Vector3(directionToPlayer.x, 0, directionToPlayer.z);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(flatDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            Debug.Log("Attacking player!");

            float distanceSqr = directionToPlayer.sqrMagnitude;
            if (distanceSqr > attackRangeSqr || !playerInSight)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"Enemy state changed: {currentState} -> {newState}");
            currentState = newState;
            
            if (newState == EnemyState.Patrol)
            {
                navAgent.speed = patrolSpeed;
                loseTargetTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
        
        void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }
            
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        
        #endregion
        
        #region Debug Visualization
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos)
            {
                return;
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);
            
            Vector3 forward = transform.forward * visionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * forward;
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * forward;
            
            Gizmos.color = playerInSight ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                    }
                }
                if (patrolPoints[patrolPoints.Length - 1] != null && patrolPoints[0] != null)
                {
                    Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
                }
            }
            
            if (currentState != EnemyState.Patrol)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1f);
            }
        }
        
        #endregion
    }
}
