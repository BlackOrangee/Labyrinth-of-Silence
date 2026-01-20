using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 

namespace Assets.Scripts
{
    /// <summary>
    /// Enemy AI with vision, hearing, chase systems AND new Attack Mechanics
    /// [FINAL POLISH] Fixed hiding logic, inactive controller error, and screen opacity
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        
        [Tooltip("Сюди перетягнути Animator з монстра")]
        public Animator animator; 
        
        private LightDetector lightDetector; 

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
        public LayerMask obstacleLayer; // [ВАЖЛИВО] Тут мають бути шари стін і меблів!

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

        // [НОВЕ] Змінна безпеки. Якщо TRUE - монстр ігнорує гравця.
        // Це знадобиться, коли ти напишеш скрипт "Interaction" для залазання в шафу.
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
            
            // [ЗМІНА] Підняли точку очей вище, щоб він не дивився з підлоги (1.6м - це рівень голови)
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

            // [НОВЕ] Перевірка: Якщо гравець сховався (isPlayerHidden) - ми його ігноруємо
            if (isPlayerHidden)
            {
                playerInSight = false; // Не бачимо
                // Якщо монстр гнався, він переходить у режим пошуку або патруля
                if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
                {
                    ChangeState(EnemyState.Search);
                }
                
                // Швидке лікування, якщо ми в безпеці
                if (currentHits > 0)
                {
                    currentHits = 0;
                    if(heartBeatAudio) heartBeatAudio.Stop();
                }
                // Очищаємо екран
                if (damageOverlay != null && damageOverlay.color.a > 0.01f)
                    damageOverlay.color = Color.Lerp(damageOverlay.color, new Color(1, 0, 0, 0), Time.deltaTime * 2f);

                UpdateAnimations();
                // Виконуємо логіку руху (щоб він не завмер, а ходив навколо)
                switch (currentState)
                {
                    case EnemyState.Patrol: PatrolBehavior(); break;
                    case EnemyState.Search: SearchBehavior(); break;
                    case EnemyState.Alert: AlertBehavior(); break;
                    default: ChangeState(EnemyState.Search); break;
                }
                return; // Виходимо з Update, щоб не дійшло до коду атаки нижче
            }

            // --- Стандартна логіка (коли гравець НЕ сховався) ---

            // Логіка відновлення
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
                    }
                }
            }

            // Плавне зникнення екрану
            if (currentHits == 0 && damageOverlay != null && damageOverlay.color.a > 0.01f)
            {
                damageOverlay.color = Color.Lerp(damageOverlay.color, new Color(1, 0, 0, 0), Time.deltaTime * 1.0f);
            }

            UpdateAnimations();

            // [НОВЕ] Перевірка для атаки:
            // 1. Дистанція підходить?
            float trueDistance = Vector3.Distance(transform.position, player.position);
            
            // 2. Чи є стіна/стіл між нами? (Захист від атаки крізь меблі)
            // Ми пускаємо промінь від голови монстра до центру гравця.
            bool isBlockedByObstacle = Physics.Linecast(transform.position + Vector3.up * 1.6f, player.position + Vector3.up, obstacleLayer);

            // Атакуємо ТІЛЬКИ якщо близько І немає перешкод
            if (trueDistance <= killDistance && !isBlockedByObstacle)
            {
                StartCoroutine(TriggerAttackSequence());
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
                case EnemyState.Search:
                    SearchBehavior();
                    break;
                case EnemyState.ScriptedEvent:
                    break;
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

            // Блокуємо керування
            if (playerMovement) playerMovement.SetMovementLock(true);
            
            if (playerCamera) 
            {
                StartCoroutine(playerCamera.ForceLookAtRoutine(faceTarget, rotationTime));
            }

            currentHits++;
            Debug.Log($"УДАР! Всього ударів: {currentHits}");

            if (animator) animator.SetTrigger("Attack");

            // Чекаємо моменту удару
            yield return new WaitForSeconds(impactWaitTime); 

            if (currentHits == 1)
            {
                // --- 1 УДАР ---
                Debug.Log("Поранення! (БАМ!)");
                
                // [ЗМІНА] Зробив екран прозорішим (0.3 замість 0.5), щоб не був таким густим
                if (damageOverlay != null) 
                {
                    damageOverlay.gameObject.SetActive(true);
                    damageOverlay.color = new Color(0.8f, 0, 0, 0.3f); 
                }
                
                if (heartBeatAudio != null) heartBeatAudio.Play();

                // [ЗМІНА] Виправлення помилки "inactive controller"
                // Перевіряємо, чи контролер існує і чи він УВІМКНЕНИЙ перед тим, як штовхати
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
                            // Ще раз перевіряємо, чи контролер не вимкнувся в процесі
                            if(controller != null && controller.enabled) 
                            {
                                controller.Move(pushDir * knockbackForce * Time.deltaTime);
                            }
                            yield return null;
                        }
                    }
                }

                recoveryTimer = damageRecoveryTime;

                // Чекаємо завершення анімації
                yield return new WaitForSeconds(1.3f); 

                // Розблокування
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

            // [ПОКРАЩЕНО] Перевірка світла + Перевірка стін
            if (lightDetector != null && lightDetector.IsLightDetected)
            {
                // LightDetector каже "світло є", але перевіримо ще раз фізично Linecast-ом
                // Чи є пряма видимість між очима монстра і гравцем?
                bool wallBetween = Physics.Linecast(transform.position + Vector3.up * 1.5f, player.position + Vector3.up, obstacleLayer);
                
                // Якщо стіни немає - значить точно бачимо
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
            // Перевірка зору звичайним рейкастом
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
            // [НОВЕ] Якщо гравець сховався - монстр ігнорує звук
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