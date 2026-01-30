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
        public float loseTargetTime = 8f;
        [Tooltip("Time to look around at destination before giving up")]
        public float lookAroundTime = 3f;

        [Header("Patrol Settings")]
        [Tooltip("Patrol points")]
        public Transform[] patrolPoints;
        [Tooltip("Wait time at each point")]
        public float waitTimeAtPoint = 2f;

        [Header("Alert Settings")]
        [Tooltip("Time to look around in alert mode before returning to patrol")]
        public float alertLookAroundTime = 4f;

        [Header("Search Settings")]
        [Tooltip("Radius around last known position to search")]
        public float searchRadius = 8f;
        [Tooltip("How long to search before giving up (seconds)")]
        public float searchDuration = 10f;
        [Tooltip("Time between choosing new random points during search")]
        public float searchPointInterval = 3f;

        [Header("Light Detection")]
        [Tooltip("Enable light-based detection")]
        public bool useLightDetection = true;
        [Tooltip("Should light trigger chase behavior")]
        public bool chaseOnLightDetection = true;

        [Header("Debug")]
        public bool showDebugGizmos = true;

        public enum EnemyState
        {
            Patrol,
            Alert,
            Chase,
            Attack,
            Search
        }
        
        private EnemyState currentState = EnemyState.Patrol;
        private NavMeshAgent navAgent;
        private int currentPatrolIndex = 0;
        private float waitTimer = 3f;
        private float loseTargetTimer = 0f;
        private Vector3 lastKnownPlayerPosition;
        private bool playerInSight = false;
        private float searchTimer = 0f;
        private float searchPointTimer = 0f;
        private Vector3 currentSearchPoint;
        private float lookAroundTimer = 0f;
        private bool reachedLastKnownPosition = false;
        private float alertLookTimer = 0f;
        private bool reachedAlertPosition = false;

        private float visionRangeSqr;
        private float attackRangeSqr;
        private float hearingRangeSqr;
        private float halfFieldOfView;
        private Vector3 eyeOffset;

        private LightDetector lightDetector;
        private bool wasLightDetected = false;

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

            if (useLightDetection)
            {
                lightDetector = GetComponent<LightDetector>();
                if (lightDetector == null)
                {
                    Debug.LogWarning($"[EnemyAI] Light detection enabled but no LightDetector component found on {gameObject.name}");
                }
            }
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
            CheckLightDetection();

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
                case EnemyState.Search:
                    SearchBehavior();
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
            if (Physics.Raycast(eyePosition, directionNormalized, out hit, distanceToPlayer, ~0, QueryTriggerInteraction.Ignore))
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
                        Debug.Log($"Vision blocked by: {hit.transform.name} (Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)})");
                    }
                }
            }
            else
            {
                if (showDebugGizmos)
                {
                    Debug.DrawRay(eyePosition, directionNormalized * visionRange, Color.yellow, 0.1f);
                }
            }

            playerInSight = false;
        }
        
        void OnPlayerSpotted()
        {
            lastKnownPlayerPosition = player.position;
            loseTargetTimer = 0f;

            if (currentState == EnemyState.Patrol || currentState == EnemyState.Alert || currentState == EnemyState.Search)
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
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Alert || currentState == EnemyState.Search)
                {
                    lastKnownPlayerPosition = soundPosition;
                    ChangeState(EnemyState.Alert);
                }
                else if (currentState == EnemyState.Chase && !playerInSight)
                {
                    lastKnownPlayerPosition = soundPosition;
                    loseTargetTimer = 0f;
                    reachedLastKnownPosition = false;
                    lookAroundTimer = 0f;
                    Debug.Log("Heard sound while chasing - updating target position");
                }
            }
        }
        
        #endregion

        #region Light Detection System

        void CheckLightDetection()
        {
            if (!useLightDetection || lightDetector == null)
            {
                return;
            }

            bool isLightDetected = lightDetector.IsLightDetected;

            if (isLightDetected && !wasLightDetected)
            {
                OnLightDetected();
            }
            else if (!isLightDetected && wasLightDetected)
            {
                OnLightLost();
            }

            wasLightDetected = isLightDetected;
        }

        void OnLightDetected()
        {
            if (!chaseOnLightDetection)
            {
                return;
            }

            if (!lightDetector.CanTriggerChase())
            {
                if (showDebugGizmos)
                {
                    Debug.Log("[EnemyAI] Light detected but intensity/range too low for chase");
                }
                return;
            }

            if (currentState == EnemyState.Patrol || currentState == EnemyState.Alert || currentState == EnemyState.Search)
            {
                lastKnownPlayerPosition = lightDetector.LastLightPosition;
                Debug.Log($"[EnemyAI] Light detected! Changing from {currentState} to Chase");
                ChangeState(EnemyState.Chase);
            }
        }

        void OnLightLost()
        {
            if (currentState == EnemyState.Chase && !playerInSight)
            {
                lastKnownPlayerPosition = lightDetector.LastLightPosition;
                Debug.Log($"[EnemyAI] Light lost - remembering position: {lastKnownPlayerPosition}");
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

            if (!navAgent.pathPending && navAgent.remainingDistance < 1.5f)
            {
                if (!reachedAlertPosition)
                {
                    reachedAlertPosition = true;
                    alertLookTimer = 0f;
                    Debug.Log("Reached alert position - looking around");
                }

                alertLookTimer += Time.deltaTime;

                if (alertLookTimer >= alertLookAroundTime)
                {
                    Debug.Log("Finished alert investigation - returning to patrol");
                    ChangeState(EnemyState.Patrol);
                }
            }
        }
        
        void ChaseBehavior()
        {
            navAgent.speed = chaseSpeed;

            bool hasVisualContact = playerInSight;
            bool hasLightContact = useLightDetection && lightDetector != null && lightDetector.IsLightDetected && lightDetector.CanTriggerChase();

            if (hasVisualContact || hasLightContact)
            {
                if (hasVisualContact)
                {
                    lastKnownPlayerPosition = player.position;
                }
                else if (hasLightContact)
                {
                    lastKnownPlayerPosition = lightDetector.LastLightPosition;
                }

                navAgent.SetDestination(lastKnownPlayerPosition);
                loseTargetTimer = 0f;
                lookAroundTimer = 0f;
                reachedLastKnownPosition = false;

                if (hasVisualContact)
                {
                    float distanceSqr = (player.position - transform.position).sqrMagnitude;
                    if (distanceSqr <= attackRangeSqr)
                    {
                        ChangeState(EnemyState.Attack);
                    }
                }
            }
            else
            {
                navAgent.SetDestination(lastKnownPlayerPosition);

                if (!navAgent.pathPending && navAgent.remainingDistance < 1.5f)
                {
                    if (!reachedLastKnownPosition)
                    {
                        reachedLastKnownPosition = true;
                        lookAroundTimer = 0f;
                        Debug.Log("Reached last known position - looking around");
                    }

                    lookAroundTimer += Time.deltaTime;

                    if (lookAroundTimer >= lookAroundTime)
                    {
                        Debug.Log("Finished looking around - starting search");
                        ChangeState(EnemyState.Search);
                        return;
                    }
                }

                loseTargetTimer += Time.deltaTime;

                if (loseTargetTimer >= loseTargetTime)
                {
                    Debug.Log("Lost player and light for too long - starting search");
                    ChangeState(EnemyState.Search);
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

        void SearchBehavior()
        {
            navAgent.speed = patrolSpeed;
            searchTimer += Time.deltaTime;
            searchPointTimer += Time.deltaTime;

            if (searchTimer >= searchDuration)
            {
                Debug.Log("Search timeout - returning to patrol");
                ChangeState(EnemyState.Patrol);
                return;
            }

            if (searchPointTimer >= searchPointInterval || (!navAgent.pathPending && navAgent.remainingDistance < 1f))
            {
                searchPointTimer = 0f;
                Vector3 randomPoint = GetRandomPointAroundPosition(lastKnownPlayerPosition, searchRadius);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
                {
                    currentSearchPoint = hit.position;
                    navAgent.SetDestination(currentSearchPoint);
                    Debug.Log($"Moving to new search point around last known position");
                }
            }
        }

        Vector3 GetRandomPointAroundPosition(Vector3 center, float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            return randomPoint;
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
                lookAroundTimer = 0f;
                reachedLastKnownPosition = false;
                alertLookTimer = 0f;
                reachedAlertPosition = false;
                GoToNearestPatrolPoint();
            }
            else if (newState == EnemyState.Search)
            {
                searchTimer = 0f;
                searchPointTimer = 0f;
                lookAroundTimer = 0f;
                reachedLastKnownPosition = false;
            }
            else if (newState == EnemyState.Chase)
            {
                loseTargetTimer = 0f;
                lookAroundTimer = 0f;
                reachedLastKnownPosition = false;
            }
            else if (newState == EnemyState.Alert)
            {
                alertLookTimer = 0f;
                reachedAlertPosition = false;
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

        void GoToNearestPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

            int nearestIndex = 0;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    continue;
                }

                float distanceSqr = (patrolPoints[i].position - transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestIndex = i;
                }
            }

            currentPatrolIndex = nearestIndex;
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
