using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 

namespace Assets.Scripts
{
    /// <summary>
    /// Enemy AI with vision, hearing, chase systems AND new Attack Mechanics
    /// [FINAL FIX] Includes Auto-Find SanityController to prevent sound overlap
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        
        [Tooltip("Сюди перетягнути Animator з монстра")]
        public Animator animator; 
        
        private LightDetector lightDetector; 

        // [НОВЕ] Посилання на контролер розуму, щоб вимикати звук психозу
        [Tooltip("Перетягни сюди об'єкт Player, на якому висить SanityController")]
        public SanityController sanityController;

        [Header("UI & Effects")]
        [Tooltip("Червона панель (Image) на весь екран.")]
        public Image damageOverlay; 
        [Tooltip("Панель смерті (Game Over).")]
        public GameObject deathScreenPanel;
        [Tooltip("Звук удару / серцебиття")]
        public AudioSource heartBeatAudio; 
        
        [Header("Vision Settings")]
        public float visionRange = 15f;
        public float fieldOfViewAngle = 110f;
        public LayerMask playerLayer;
        public LayerMask obstacleLayer; 

        [Header("Hearing Settings")]
        public float hearingRange = 20f;

        [Header("Chase Settings")]
        public float attackRange = 2f; 
        public float chaseSpeed = 5f; 
        public float patrolSpeed = 2.5f; 
        public float loseTargetTime = 5f;

        [Header("Patrol Settings")]
        public Transform[] patrolPoints;
        public float waitTimeAtPoint = 2f;

        [Header("Search Settings")]
        public float searchRadius = 8f;
        public float searchDuration = 10f;
        public float searchPointInterval = 3f;

        [Header("Death & Attack Mechanics")]
        public float killDistance = 1.3f; 
        public Transform faceTarget;
        public float rotationTime = 0.4f;

        [Tooltip("Скільки секунд треба бути ПОЗА ЗОРОМ монстра, щоб екран почав очищуватись")]
        public float damageRecoveryTime = 4f; 
        
        [Tooltip("Час (в секундах), який монстр чекає після першого удару")]
        public float stunTimeAfterHit = 3.5f; 

        [Header("Physics & Impact")]
        [Tooltip("Затримка перед почервонінням екрану (синхронізація з анімацією)")]
        public float impactWaitTime = 1.0f; 
        
        [Tooltip("Сила відкидання гравця")]
        public float knockbackForce = 8f; 

        // Змінна безпеки. Якщо TRUE - монстр ігнорує гравця.
        [Tooltip("Якщо гравець сховався в шафі/під столом, став цю галочку в TRUE")]
        public bool isPlayerHidden = false;

        [Tooltip("Чи показувати дебаг лінії")]
        public bool showDebugGizmos = true;
        
        public enum EnemyState
        {
            Patrol,
            Alert,
            Chase,
            Attack,
            Search,
            ScriptedEvent
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

        private float visionRangeSqr;
        private float attackRangeSqr;
        private float hearingRangeSqr;
        private float halfFieldOfView;
        private Vector3 eyeOffset;

        private PlayerMovement playerMovement;
        private CameraController playerCamera;
        
        private bool isEventActive = false; 
        private int currentHits = 0; 
        private float recoveryTimer = 0f; 

        void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();
            lightDetector = GetComponent<LightDetector>();

            // [ВИПРАВЛЕНО] Автоматичний пошук SanityController
            // Це гарантує, що ми зможемо вимкнути звук психозу, навіть якщо забули про Inspector
            if (sanityController == null)
            {
                if (player != null)
                {
                    sanityController = player.GetComponent<SanityController>();
                }
                else
                {
                    // Пробуємо знайти за типом
                    sanityController = Object.FindFirstObjectByType<SanityController>();
                }
            }

            if (damageOverlay != null)
            {
                damageOverlay.gameObject.SetActive(true); 
                damageOverlay.color = new Color(1, 0, 0, 0); 
            }

            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(false);
            }

            if (navAgent == null)
            {
                Debug.LogError("NavMeshAgent not found!");
                enabled = false;
                return;
            }

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
                playerCamera = player.GetComponentInChildren<CameraController>();
            }

            if (faceTarget == null)
            {
                Transform head = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head");
                if(head != null) faceTarget = head;
                else faceTarget = transform; 
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
            
            eyeOffset = Vector3.up * 1.6f;
        }
        
        void OnDestroy()
        {
            SoundManager.OnSoundEmitted -= OnSoundHeard;
        }
        
        void Update()
        {
            if (!player) return;
            if (isEventActive) return;

            // Логіка Схованки
            if (isPlayerHidden)
            {
                playerInSight = false; 
                
                if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
                {
                    ChangeState(EnemyState.Search);
                }
                
                if (currentHits > 0)
                {
                    currentHits = 0;
                    if(heartBeatAudio) heartBeatAudio.Stop();
                    // [ВИПРАВЛЕНО] Якщо ми вилікувались в схованці - вмикаємо назад психоз
                    if(sanityController != null) sanityController.SetHeartbeatMute(false);
                }
                
                if (damageOverlay != null && damageOverlay.color.a > 0.01f)
                    damageOverlay.color = Color.Lerp(damageOverlay.color, new Color(1, 0, 0, 0), Time.deltaTime * 2f);

                UpdateAnimations();
                
                switch (currentState)
                {
                    case EnemyState.Patrol: PatrolBehavior(); break;
                    case EnemyState.Search: SearchBehavior(); break;
                    case EnemyState.Alert: AlertBehavior(); break;
                    default: ChangeState(EnemyState.Search); break;
                }
                return; 
            }

            // --- Звичайна логіка ---

            if (currentHits == 1)
            {
                if (playerInSight)
                {
                    recoveryTimer = damageRecoveryTime; 
                }
                else
                {
                    recoveryTimer -= Time.deltaTime;
                    if (recoveryTimer <= 0)
                    {
                        Debug.Log("Гравець сховався і відновився!");
                        currentHits = 0;
                        if(heartBeatAudio != null) heartBeatAudio.Stop();
                        // [ВИПРАВЛЕНО] Відновилися - повертаємо звук психозу
                        if(sanityController != null) sanityController.SetHeartbeatMute(false);
                    }
                }
            }

            if (currentHits == 0 && damageOverlay != null && damageOverlay.color.a > 0.01f)
            {
                damageOverlay.color = Color.Lerp(damageOverlay.color, new Color(1, 0, 0, 0), Time.deltaTime * 1.0f);
            }

            UpdateAnimations();

            float trueDistance = Vector3.Distance(transform.position, player.position);
            bool isBlockedByObstacle = Physics.Linecast(transform.position + Vector3.up * 1.6f, player.position + Vector3.up, obstacleLayer);

            if (trueDistance <= killDistance && !isBlockedByObstacle)
            {
                StartCoroutine(TriggerAttackSequence());
                return;
            }

            CheckVision();
            
            switch (currentState)
            {
                case EnemyState.Patrol: PatrolBehavior(); break;
                case EnemyState.Alert: AlertBehavior(); break;
                case EnemyState.Chase: ChaseBehavior(); break;
                case EnemyState.Attack: AttackBehavior(); break;
                case EnemyState.Search: SearchBehavior(); break;
                case EnemyState.ScriptedEvent: break;
            }
        }

        void UpdateAnimations()
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", navAgent.velocity.magnitude);
            }
        }
        
        #region Core Behaviors
        
        IEnumerator TriggerAttackSequence()
        {
            isEventActive = true;
            ChangeState(EnemyState.ScriptedEvent);
            
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;

            // [ВИПРАВЛЕНО] Глушимо психоз перед початком атаки
            if(sanityController != null) sanityController.SetHeartbeatMute(true);

            if (playerMovement) playerMovement.SetMovementLock(true);
            
            if (playerCamera) 
            {
                StartCoroutine(playerCamera.ForceLookAtRoutine(faceTarget, rotationTime));
            }

            currentHits++;
            Debug.Log($"УДАР! Всього ударів: {currentHits}");

            if (animator) animator.SetTrigger("Attack");

            yield return new WaitForSeconds(impactWaitTime); 

            if (currentHits == 1)
            {
                // --- 1 УДАР ---
                Debug.Log("Поранення! (БАМ!)");
                
                if (damageOverlay != null) 
                {
                    damageOverlay.gameObject.SetActive(true);
                    damageOverlay.color = new Color(0.8f, 0, 0, 0.3f); 
                }
                
                if (heartBeatAudio != null) heartBeatAudio.Play();

                if (player != null)
                {
                    CharacterController controller = player.GetComponent<CharacterController>();
                    
                    if (controller != null && controller.enabled)
                    {
                        Vector3 pushDir = player.position - transform.position;
                        pushDir.y = 0; 
                        pushDir.Normalize();

                        float timer = 0;
                        while(timer < 0.2f) 
                        {
                            timer += Time.deltaTime;
                            if(controller.enabled) controller.Move(pushDir * knockbackForce * Time.deltaTime);
                            yield return null;
                        }
                    }
                }

                recoveryTimer = damageRecoveryTime;

                yield return new WaitForSeconds(1.3f); 

                if (playerMovement) playerMovement.SetMovementLock(false);
                if (playerCamera) playerCamera.SetInputLock(false);
                
                Debug.Log("Монстр в ступорі...");
                yield return new WaitForSeconds(stunTimeAfterHit);
                
                isEventActive = false;
                ChangeState(EnemyState.Chase); 
                navAgent.isStopped = false;
            }
            else
            {
                // --- 2 УДАР (СМЕРТЬ) ---
                
                if (heartBeatAudio != null) heartBeatAudio.Stop();
                // Глушимо психоз назавжди, бо ми мертві
                if(sanityController != null) sanityController.SetHeartbeatMute(true);

                if (damageOverlay != null) 
                    damageOverlay.color = new Color(0.6f, 0, 0, 1f);

                yield return new WaitForSeconds(1.0f);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (deathScreenPanel != null)
                {
                    deathScreenPanel.SetActive(true);
                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }

        #region Standard AI Logic

        void CheckVision()
        {
            if (player == null) return;

            if (lightDetector != null && lightDetector.IsLightDetected)
            {
                bool wallBetween = Physics.Linecast(transform.position + Vector3.up * 1.5f, player.position + Vector3.up, obstacleLayer);
                
                if (!wallBetween)
                {
                    lastKnownPlayerPosition = player.position;
                    playerInSight = true; 

                    if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
                    {
                        if(showDebugGizmos) Debug.Log("Викрито світлом! Починаю погоню.");
                        ChangeState(EnemyState.Chase);
                    }
                    return; 
                }
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
            RaycastHit hit;
            if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out hit, distanceToPlayer, playerLayer | obstacleLayer))
            {
                if (hit.transform == player || hit.transform.IsChildOf(player))
                {
                    playerInSight = true;
                    OnPlayerSpotted();
                    return;
                }
            }
            playerInSight = false;
        }
        
        void OnPlayerSpotted()
        {
            lastKnownPlayerPosition = player.position;
            loseTargetTimer = 0f;

            if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        
        void OnSoundHeard(Vector3 soundPosition, float soundIntensity, GameObject source)
        {
            if (source == gameObject) return;
            if (isPlayerHidden) return; 

            float distSqr = (soundPosition - transform.position).sqrMagnitude;
            if (distSqr <= (hearingRange * soundIntensity) * (hearingRange * soundIntensity))
            {
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Search)
                {
                    lastKnownPlayerPosition = soundPosition;
                    ChangeState(EnemyState.Alert);
                }
            }
        }
        
        void PatrolBehavior()
        {
            navAgent.speed = patrolSpeed;
            
            if (navAgent.remainingDistance < 0.5f && !navAgent.pathPending)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    waitTimer = waitTimeAtPoint;
                    GoToNextPatrolPoint();
                }
            }
        }
        
        void AlertBehavior()
        {
            navAgent.speed = patrolSpeed;
            navAgent.SetDestination(lastKnownPlayerPosition);
            
            if (!navAgent.pathPending && navAgent.remainingDistance < 1f)
            {
                ChangeState(EnemyState.Search);
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
            }
            else
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
                loseTargetTimer += Time.deltaTime;

                if (loseTargetTimer >= loseTargetTime)
                {
                    ChangeState(EnemyState.Search);
                }
            }
        }
        
        void AttackBehavior()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            
            if (dir.sqrMagnitude > attackRangeSqr)
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
                ChangeState(EnemyState.Patrol);
                return;
            }

            if (searchPointTimer >= searchPointInterval || navAgent.remainingDistance < 0.5f)
            {
                searchPointTimer = 0f;
                Vector3 randomPoint = lastKnownPlayerPosition + (Random.insideUnitSphere * searchRadius);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, searchRadius, NavMesh.AllAreas))
                {
                    navAgent.SetDestination(hit.position);
                }
            }
        }

        void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (newState == EnemyState.Patrol) GoToNextPatrolPoint();
            else if (newState == EnemyState.Search)
            {
                searchTimer = 0f;
                searchPointTimer = 0f;
            }
        }
        
        void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        #endregion 
        
        #endregion

        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Vector3 left = Quaternion.Euler(0, -fieldOfViewAngle/2, 0) * transform.forward * visionRange;
            Vector3 right = Quaternion.Euler(0, fieldOfViewAngle/2, 0) * transform.forward * visionRange;
            Gizmos.DrawLine(transform.position, transform.position + left);
            Gizmos.DrawLine(transform.position, transform.position + right);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.black; 
            Gizmos.DrawWireSphere(transform.position, killDistance);
        }
    }
}