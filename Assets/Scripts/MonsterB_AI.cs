using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Assets.Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterB_AI : MonoBehaviour
    {
        #region 1. References (Посилання)
        [Header("References (Посилання)")]
        [Tooltip("Посилання на гравця")]
        public Transform player;
        [Tooltip("Аніматор монстра")]
        public Animator animator;
        [Tooltip("Контролер ПСИХОЗУ")]
        public SanityController sanityController;
        #endregion

        #region 2. UI & Effects (Ефекти)
        [Header("UI & Effects (Інтерфейс та ефекти)")]
        [Tooltip("Картинка пошкодження (червоний екран)")]
        public Image damageOverlay;
        [Tooltip("Панель екрану смерті")]
        public GameObject deathScreenPanel;
        [Tooltip("Звук серцебиття")]
        public AudioSource heartBeatAudio;
        #endregion

        #region 3. Audio Settings (Налаштування Звуку)
        [Header("Audio Files (Звукові файли)")]
        [Tooltip("Файл: Один крок (НЕ ЛУП!). Використовуй короткий звук удару ногою.")]
        public AudioClip stepSound;
        [Tooltip("Файл: помітив гравця.mp3")]
        public AudioClip spotSound;
        [Tooltip("Файл: удар.mp3")]
        public AudioClip attackSound;
        [Tooltip("Файл: рик монстра.mp3 (фоновий звук)")]
        public AudioClip patrolSound;

        [Header("Audio Sources (Прив'яжи вручну в Inspector)")]
        [Tooltip("Створення на монстрі AudioSource для ГОЛОСУ")]
        public AudioSource voiceSource; 
        [Tooltip("Створення на монстрі AudioSource для НІГ")]
        public AudioSource feetSource;

        #endregion

        #region 4. Sense Settings (Налаштування чуття)
        [Header("Blind Sense Settings (Налаштування чуття)")]
        [Tooltip("Радіус (Червоний), в якому монстр гарантовано відчуває гравця")]
        public float proximitySenseRange = 3.0f;
        [Tooltip("Радіус (Жовтий), як далеко чує кроки")]
        public float hearingRange = 25f;
        #endregion

        #region 5. Movement Settings (Рух)
        [Header("Movement Settings (Налаштування руху)")]
        [Tooltip("Швидкість патрулювання")]
        public float patrolSpeed = 1.5f;
        [Tooltip("Швидкість погоні")]
        public float chaseSpeed = 3.5f;
        [Tooltip("Час очікування на точці патрулювання")]
        public float waitTimeAtPoint = 2f;
        [Tooltip("Час пам'яті про позицію гравця")]
        public float memoryDuration = 5f;
        #endregion

        #region 6. Attack Settings (Атака)
        [Header("Attack Settings (Налаштування атаки)")]
        [Tooltip("Дистанція (Синя), з якої починається атака")]
        public float attackRange = 1.8f;
        
        [Tooltip("Точний час, коли має пролунати звук удару (сек)")]
        public float attackSoundDelay = 0.3f; 

        [Tooltip("Затримка перед нанесенням шкоди (синхронізація з анімацією).")]
        public float damageDelay = 0.4f; 
        
        [Tooltip("Час блокування гравця після першого удару (ШОК)")]
        public float playerStunDuration = 2.0f;
        
        [Tooltip("Час, який монстр стоїть і гарчить після 1-го удару (дає фору гравцю)")]
        public float monsterTauntDuration = 3.0f; 
        
        [Tooltip("Скільки секунд гравець бачить монстра при смертельному ударі, перш ніж з'явиться екран смерті")]
        public float deathVisibilityDuration = 0.01f; 

        [Tooltip("Час відновлення гравця (зникнення червоного екрану), якщо втік")]
        public float recoveryTime = 10f;
        #endregion

        #region 7. Patrol Points
        [Header("Patrol (Патрулювання)")]
        public Transform[] patrolPoints;
        #endregion

        #region 8. Debug Variables
        [Header("Debug")]
        public bool showDebugGizmos = true;
        public bool isPlayerHidden = false;
        #endregion

        public enum AIState
        {
            Patrol,
            Alert,
            Chase,
            Attack,
            Taunt,
            Kill
        }
        [SerializeField] private AIState currentState = AIState.Patrol;
        private NavMeshAgent navAgent;
        private int currentPatrolIndex = 0;
        private float waitTimer = 0f;
        private float memoryTimer = 0f;
        private float hitRecoveryTimer = 0f;
        private Vector3 targetPosition;
        private int currentHits = 0;
        
        private PlayerMovement playerMovement;
        private CameraController playerCamera;
        void Start()
        {
            AudioListener.volume = 1f;

            navAgent = GetComponent<NavMeshAgent>();
            navAgent.updateRotation = true;
            navAgent.speed = patrolSpeed;

            navAgent.acceleration = 60f; 

            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
                playerCamera = player.GetComponentInChildren<CameraController>();
                if (sanityController == null) sanityController = player.GetComponent<SanityController>();
            }

            if (voiceSource != null && patrolSound != null)
            {
                voiceSource.clip = patrolSound;
                voiceSource.loop = true;
                voiceSource.Play();
            }

            SoundManager.OnSoundEmitted += OnSoundHeard;

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                GoToNextPatrolPoint();
            }

            if (damageOverlay) damageOverlay.color = Color.clear;
        }
        void OnDestroy()
        {
            SoundManager.OnSoundEmitted -= OnSoundHeard;

            if (voiceSource) voiceSource.Stop();
            if (feetSource) feetSource.Stop();
        }
        void Update()
        {
            if (!player) return;

            if (currentState == AIState.Kill) return;

            if (currentHits == 1 && currentState != AIState.Attack && currentState != AIState.Taunt)
            {
                hitRecoveryTimer -= Time.deltaTime;

                if (damageOverlay)
                {
                    float alpha = Mathf.Lerp(0, 0.5f, hitRecoveryTimer / recoveryTime);
                    damageOverlay.color = new Color(1, 0, 0, alpha);
                }

                if (hitRecoveryTimer <= 0)
                {
                    currentHits = 0;
                    if (heartBeatAudio) heartBeatAudio.Stop();
                    Debug.Log("Monster-B: Гравець втік і відновився.");
                }
            }

            UpdateAnimator();

            if (currentState == AIState.Attack || currentState == AIState.Taunt) return;

            if (isPlayerHidden)
            {
                if (currentState == AIState.Chase)
                {
                    currentState = AIState.Patrol;
                    GoToNearestPatrolPoint();
                }
            }
            else
            {
                CheckProximity();
            }
            switch (currentState)
            {
                case AIState.Patrol:
                    PatrolBehavior();
                    break;
                case AIState.Alert:
                    AlertBehavior();
                    break;
                case AIState.Chase:
                    ChaseBehavior();
                    break;
            }
        }
        void UpdateAnimator()
        {
            if (animator != null)
            {
            float currentSpeed = navAgent.isStopped ? 0 : navAgent.velocity.magnitude;

            animator.SetFloat("Speed", currentSpeed, 0.05f, Time.deltaTime);
        }
        }
        public void PlayStepSound()
        {
            if (navAgent.velocity.magnitude > 0.1f && stepSound != null && feetSource != null)
            {
                float currentSpeed = navAgent.velocity.magnitude;
                
                float pitch = Mathf.Lerp(0.9f, 1.15f, currentSpeed / chaseSpeed);
                
                feetSource.pitch = pitch + Random.Range(-0.05f, 0.05f);

                feetSource.PlayOneShot(stepSound);
            }
        }

        #region Sensory Systems (Чуття)
        void CheckProximity()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= proximitySenseRange)
            {
                targetPosition = player.position;
                memoryTimer = memoryDuration;

                if (currentState != AIState.Chase && currentState != AIState.Attack)
                {
                    if (spotSound && voiceSource && currentState != AIState.Chase)
                    {
                        voiceSource.PlayOneShot(spotSound);
                    }
                    currentState = AIState.Chase;
                }
            }

            if (currentState == AIState.Chase && distanceToPlayer <= attackRange)
            {
                StartCoroutine(ExecuteAttack());
            }
        }
        void OnSoundHeard(Vector3 position, float intensity, GameObject source)
        {
            if (source == gameObject || isPlayerHidden || currentState == AIState.Attack || currentState == AIState.Kill) return;

            float distanceToSound = Vector3.Distance(transform.position, position);

            if (distanceToSound <= hearingRange * intensity)
            {
                targetPosition = position;
                
                if (currentState == AIState.Patrol)
                {
                    currentState = AIState.Alert;
                    navAgent.SetDestination(targetPosition);
                }
                else if (currentState == AIState.Chase)
                {
                    navAgent.SetDestination(targetPosition);
                }
            }
        }

        #endregion

        #region Behaviors (Поведінка)
        void PatrolBehavior()
        {
            navAgent.speed = patrolSpeed;
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTimeAtPoint)
                {
                    GoToNextPatrolPoint();
                    waitTimer = 0f;
                }
            }
        }
        void AlertBehavior()
        {
            navAgent.speed = patrolSpeed;
            if (!navAgent.pathPending && navAgent.remainingDistance < 1f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= 3f) 
                {
                    currentState = AIState.Patrol;
                    GoToNearestPatrolPoint();
                    waitTimer = 0f;
                }
            }
        }
        void ChaseBehavior()
        {
            navAgent.speed = chaseSpeed;
            navAgent.SetDestination(targetPosition);

            memoryTimer -= Time.deltaTime;
            if (memoryTimer <= 0)
            {
                currentState = AIState.Alert; 
            }
        }

        #endregion

        #region Attack Logic (Атака і Смерть)
        IEnumerator ExecuteAttack()
        {
            currentState = AIState.Attack;
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;

            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

            if (animator) animator.SetTrigger("Attack");

            Debug.Log("Monster-B: Початок атаки (Замах)...");

            yield return new WaitForSeconds(attackSoundDelay);

            if (attackSound && voiceSource)
            {
                voiceSource.PlayOneShot(attackSound);
            }

            float remainingTime = Mathf.Max(0f, damageDelay - attackSoundDelay);
            yield return new WaitForSeconds(remainingTime);

            float distance = Vector3.Distance(transform.position, player.position);
            
            if (distance <= attackRange + 0.5f)
            {
                currentHits++;
                Debug.Log($"Monster-B: УДАР {currentHits} ПОПАВ!");

                if (currentHits == 1)
                {
                    StartCoroutine(HandleFirstHitSequence());
                }
                else if (currentHits >= 2)
                {
                    StartCoroutine(HandleDeathSequence());
                    yield break;
                }
            }
            else
            {
                Debug.Log("Monster-B: Промах!");
                yield return new WaitForSeconds(0.5f);
                currentState = AIState.Chase;
                navAgent.isStopped = false;
            }
        }
        IEnumerator HandleFirstHitSequence()
        {
            currentState = AIState.Taunt;
            if (heartBeatAudio) heartBeatAudio.Play();
            
            if (damageOverlay)
            {
                damageOverlay.gameObject.SetActive(true);
                damageOverlay.color = new Color(1, 0, 0, 0.5f);
            }

            if (playerMovement) playerMovement.SetMovementLock(true);
            Debug.Log("Player: ШОК! Рух заблоковано.");

            hitRecoveryTimer = recoveryTime;

            yield return new WaitForSeconds(playerStunDuration);

            if (playerMovement) playerMovement.SetMovementLock(false);
            Debug.Log("Player: Рух відновлено. ТІКАЙ!");

            float remainingWait = monsterTauntDuration - playerStunDuration;
            if (remainingWait > 0) yield return new WaitForSeconds(remainingWait);

            currentState = AIState.Chase;
            navAgent.isStopped = false;
        }
        IEnumerator HandleDeathSequence()
        {
            currentState = AIState.Kill;
            Debug.Log("Monster-B: ГРАВЦЯ ВБИТО.");

            navAgent.isStopped = true;
            if (playerMovement) playerMovement.SetMovementLock(true);
            if (heartBeatAudio) heartBeatAudio.Stop();
            if (damageOverlay) damageOverlay.gameObject.SetActive(false);

            var skillCheck = FindObjectOfType<SkillCheckSystem>(); 
            if (skillCheck != null) skillCheck.ForceStop();

            if (deathVisibilityDuration > 0)
            {
                yield return new WaitForSeconds(deathVisibilityDuration);
            }

            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(true);
                VideoPlayer vp = deathScreenPanel.GetComponentInChildren<VideoPlayer>();
                if (vp) vp.Play();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield break;
            }

            yield return null; 

            AudioListener.volume = 0f;
            
            if (voiceSource) voiceSource.Stop();
            if (feetSource) feetSource.Stop();
        }

        #endregion

        #region Navigation Helpers
        void GoToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        void GoToNearestPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            
            float nearestDist = float.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float d = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestIndex = i;
                }
            }
            currentPatrolIndex = nearestIndex;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        #endregion
        public void TriggerChaseToLocation(Vector3 location)
        {
            Debug.Log("Monster-B: Чую вибух генератора! Біжу туди!");
            
            if (currentState == AIState.Patrol || currentState == AIState.Alert)
            {
                targetPosition = location;
                navAgent.SetDestination(targetPosition);
                currentState = AIState.Chase;

                memoryTimer = 10f;
                
                if (spotSound && voiceSource) voiceSource.PlayOneShot(spotSound);
            }
        }
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, proximitySenseRange);

            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}