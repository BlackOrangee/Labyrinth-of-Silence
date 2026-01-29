using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 
using UnityEngine.Video; 

namespace Assets.Scripts
{
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public Animator animator; 
        private LightDetector lightDetector; 
        public SanityController sanityController;

        [Header("UI & Effects")]
        public Image damageOverlay; 
        public GameObject deathScreenPanel;
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
        public float chaseSpeed = 3.8f; 
        public float patrolSpeed = 1.5f; 
        public float loseTargetTime = 5f;

        [Header("Patrol Settings")]
        public Transform[] patrolPoints;
        public float waitTimeAtPoint = 1f;

        [Header("Search Settings")]
        public float searchRadius = 8f;
        public float searchDuration = 3f;
        public float searchPointInterval = 3f;

        [Header("Death & Attack Mechanics")]
        public float killDistance = 1.3f; 
        public Transform faceTarget;
        public float rotationTime = 0.4f;

        public float damageRecoveryTime = 4f; 
        public float stunTimeAfterHit = 3.5f; 

        [Header("Physics & Impact")]
        public float impactWaitTime = 0.5f; // [FIX] Зменшив час до удару, щоб було динамічніше
        public float knockbackForce = 8f; 
        public float playerLockDuration = 1.3f;

        public bool isPlayerHidden = false;
        public bool showDebugGizmos = true;
        
        public enum EnemyState { Patrol, Alert, Chase, Attack, Search, ScriptedEvent }
        
        private EnemyState currentState = EnemyState.Patrol;
        private NavMeshAgent navAgent;
        private int currentPatrolIndex = 0;
        private float waitTimer = 0f; 
        private float loseTargetTimer = 0f;
        private Vector3 lastKnownPlayerPosition;
        private bool playerInSight = false;
        private float searchTimer = 0f;
        
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
            // [FIX] Вимикаємо авто-гальмування і авто-поворот, щоб контролювати це самим
            navAgent.autoBraking = true; 
            navAgent.updateRotation = true; 

            lightDetector = GetComponent<LightDetector>();

            if (sanityController == null)
            {
                if (player != null) sanityController = player.GetComponent<SanityController>();
                else sanityController = Object.FindFirstObjectByType<SanityController>();
            }

            if (damageOverlay != null)
            {
                damageOverlay.gameObject.SetActive(true); 
                damageOverlay.color = new Color(1, 0, 0, 0); 
            }

            if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

            if (navAgent == null) { enabled = false; return; }

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
            waitTimer = waitTimeAtPoint;

            if (patrolPoints != null && patrolPoints.Length > 0) GoToNextPatrolPoint();

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

            if (isPlayerHidden)
            {
                playerInSight = false; 
                if (currentState == EnemyState.Chase || currentState == EnemyState.Attack) ChangeState(EnemyState.Search);
                
                if (currentHits > 0)
                {
                    currentHits = 0;
                    if(heartBeatAudio) heartBeatAudio.Stop();
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

            // Відновлення
            if (currentHits == 1)
            {
                if (playerInSight) recoveryTimer = damageRecoveryTime; 
                else
                {
                    recoveryTimer -= Time.deltaTime;
                    if (recoveryTimer <= 0)
                    {
                        currentHits = 0;
                        if(heartBeatAudio != null) heartBeatAudio.Stop();
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
                float currentSpeed = navAgent.isStopped ? 0f : navAgent.velocity.magnitude;
                animator.SetFloat("Speed", currentSpeed);
            }
        }
        
        IEnumerator TriggerAttackSequence()
        {
            isEventActive = true;
            ChangeState(EnemyState.ScriptedEvent);
            
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            // [FIX] Вимикаємо авто-поворот, щоб він не крутився сам
            navAgent.updateRotation = false;

            if (animator != null) 
            {
                animator.SetFloat("Speed", 0f);
                // [FIX] Вмикаємо Root Motion, щоб удар мав фізичну вагу
                animator.applyRootMotion = true; 
            }

            if(sanityController != null) sanityController.SetHeartbeatMute(true);
            if (playerMovement) playerMovement.SetMovementLock(true);
            
            if (playerCamera) StartCoroutine(playerCamera.ForceLookAtRoutine(faceTarget, rotationTime));

            currentHits++;
            Debug.Log($"УДАР! Всього ударів: {currentHits}");

            if (animator) animator.SetTrigger("Attack");

            yield return new WaitForSeconds(impactWaitTime); 

            if (currentHits == 1)
            {
                // 1 УДАР
                if (damageOverlay != null) 
                {
                    damageOverlay.gameObject.SetActive(true);
                    damageOverlay.color = new Color(0.8f, 0, 0, 0.3f); 
                }
                if (heartBeatAudio != null) heartBeatAudio.Play();

                // Відкидання
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
                yield return new WaitForSeconds(playerLockDuration); 

                if (playerMovement) playerMovement.SetMovementLock(false);
                if (playerCamera) playerCamera.SetInputLock(false);
                
                navAgent.isStopped = true; 
                if (animator != null) animator.SetFloat("Speed", 0f);

                yield return new WaitForSeconds(stunTimeAfterHit);
                
                // [FIX] Повертаємо керування агенту
                if (animator != null) animator.applyRootMotion = false;
                navAgent.updateRotation = true;
                navAgent.isStopped = false; 
                
                isEventActive = false;
                ChangeState(EnemyState.Chase); 
                navAgent.SetDestination(player.position); 
            }
            else
            {
                // 2 УДАР (СМЕРТЬ)
                if (GlobalSoundManager.Instance != null) GlobalSoundManager.Instance.FadeOutAllSounds(1f);
                if (heartBeatAudio != null) heartBeatAudio.Stop();
                if(sanityController != null) sanityController.SetHeartbeatMute(true);

                if (damageOverlay != null) damageOverlay.color = new Color(0.6f, 0, 0, 1f);

                yield return new WaitForSeconds(1.0f);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (deathScreenPanel != null)
                {
                    CanvasGroup cg = deathScreenPanel.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 0f; 
                    deathScreenPanel.SetActive(true); 
                    VideoPlayer vp = deathScreenPanel.GetComponentInChildren<VideoPlayer>();
                    if (vp != null) { vp.Prepare(); while (!vp.isPrepared) yield return null; vp.Play(); }
                    if (cg != null) cg.alpha = 1f; 
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

            float distanceSqr = (player.position - transform.position).sqrMagnitude;

            // [FIX] АБСОЛЮТНИЙ ЗІР НА 2.5 МЕТРИ (Щоб не був сліпим впритул)
            if (distanceSqr < 6.25f) // 2.5 * 2.5
            {
                lastKnownPlayerPosition = player.position;
                playerInSight = true;
                OnPlayerSpotted();
                return;
            }

            if (lightDetector != null && lightDetector.IsLightDetected)
            {
                bool wallBetween = Physics.Linecast(transform.position + Vector3.up * 1.5f, player.position + Vector3.up, obstacleLayer);
                if (!wallBetween)
                {
                    lastKnownPlayerPosition = player.position;
                    playerInSight = true; 
                    if (currentState != EnemyState.Chase && currentState != EnemyState.Attack) ChangeState(EnemyState.Chase);
                    return; 
                }
            }

            Vector3 eyePosition = transform.position + eyeOffset;
            Vector3 directionToPlayer = player.position - eyePosition;

            if (distanceSqr > visionRangeSqr) { playerInSight = false; return; }

            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > halfFieldOfView) { playerInSight = false; return; }

            if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out RaycastHit hit, Mathf.Sqrt(distanceSqr), playerLayer | obstacleLayer))
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
            if (currentState != EnemyState.Chase && currentState != EnemyState.Attack) ChangeState(EnemyState.Chase);
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
            if (!navAgent.pathPending && navAgent.remainingDistance <= 0.8f)
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
            if (!navAgent.pathPending && navAgent.remainingDistance < 1f) ChangeState(EnemyState.Search);
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
                if (loseTargetTimer >= loseTargetTime) ChangeState(EnemyState.Search);
            }
        }
        
        void AttackBehavior()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            // [FIX] Ручний плавний поворот, коли NavMesh поворот вимкнено
            if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            
            if (dir.sqrMagnitude > attackRangeSqr) ChangeState(EnemyState.Chase);
        }
        
        void SearchBehavior()
        {
            navAgent.isStopped = true;
            if (animator) animator.SetFloat("Speed", 0f);

            searchTimer += Time.deltaTime;
            // Просто стоїмо і слухаємо, без обертання

            if (searchTimer >= searchDuration) ChangeState(EnemyState.Patrol);
        }

        void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (newState == EnemyState.Patrol) 
            {
                navAgent.isStopped = false; 
                navAgent.updateRotation = true; // [FIX] Повертаємо контроль
                navAgent.ResetPath();
                navAgent.speed = patrolSpeed; 
                loseTargetTimer = 0f;
                waitTimer = 0f;
                GoToNextPatrolPoint(); 
            }
            else if (newState == EnemyState.Search)
            {
                searchTimer = 0f;
            }
        }
        
        void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            if (currentPatrolIndex >= patrolPoints.Length) currentPatrolIndex = 0;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        #endregion 
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRange);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.black; Gizmos.DrawWireSphere(transform.position, killDistance);
        }
    }
}