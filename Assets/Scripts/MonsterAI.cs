using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Assets.Scripts
{
    /// <summary>
    /// Unified monster AI for all monster variants.
    ///
    /// Sighted monster  : visionRange > 0, useLightDetection = true,  hitsToKill = 2-3
    /// Blind monster    : visionRange = 0, useLightDetection = false, hitsToKill = 2
    ///
    /// Both monsters share the full state machine including Search (wander on target loss).
    /// Audio is resolved automatically: MonsterAudio component takes priority over inline clips.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI : MonoBehaviour
    {
        // ── References ───────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Player transform. Auto-found by 'Player' tag if left empty.")]
        public Transform player;
        [Tooltip("Monster animator.")]
        public Animator animator;
        [Tooltip("Sanity controller on the player. Auto-found if left empty.")]
        public SanityController sanityController;

        // ── UI & Effects ─────────────────────────────────────────────
        [Header("UI & Effects")]
        [Tooltip("Red damage overlay image.")]
        public Image damageOverlay;
        [Tooltip("Death screen panel (should contain a VideoPlayer child).")]
        public GameObject deathScreenPanel;
        [Tooltip("Heartbeat audio source on the player.")]
        public AudioSource heartBeatAudio;

        // ── Inline Audio ─────────────────────────────────────────────
        [Header("Inline Audio (used when no MonsterAudio component is present)")]
        [Tooltip("AudioSource for voice/roar sounds (looped patrol roar + one-shots).")]
        public AudioSource voiceSource;
        [Tooltip("AudioSource for footstep sounds. Called from animation events via PlayStepSound().")]
        public AudioSource feetSource;
        [Tooltip("Single footstep clip played from animation events.")]
        public AudioClip stepSound;
        [Tooltip("Played once when the monster spots the player.")]
        public AudioClip spotSound;
        [Tooltip("Played on attack swing.")]
        public AudioClip attackSound;
        [Tooltip("Looped ambient roar during patrol.")]
        public AudioClip patrolLoopSound;

        // ── Detection ────────────────────────────────────────────────
        [Header("Detection")]
        [Tooltip("FOV vision range in meters. Set to 0 to disable cone vision (blind monster).")]
        public float visionRange = 15f;
        [Tooltip("Field of view angle in degrees. Ignored when visionRange = 0.")]
        [Range(10f, 360f)]
        public float fieldOfViewAngle = 110f;
        [Tooltip("Guaranteed 360° detection radius. Acts as proximity sense for all monsters.")]
        public float closeDetectionRange = 2.5f;
        [Tooltip("Detect player's lantern. Disable for blind monsters.")]
        public bool useLightDetection = true;
        [Tooltip("Layers considered solid obstacles for line-of-sight checks.")]
        public LayerMask obstacleLayer;

        // ── Hearing ──────────────────────────────────────────────────
        [Header("Hearing")]
        [Tooltip("Maximum hearing range at full sound intensity.")]
        public float hearingRange = 20f;

        // ── Movement ─────────────────────────────────────────────────
        [Header("Movement")]
        public float patrolSpeed = 1.5f;
        public float chaseSpeed = 3.8f;
        [Tooltip("NavMeshAgent acceleration. Increase for snappier response.")]
        public float navAcceleration = 8f;

        // ── Patrol ───────────────────────────────────────────────────
        [Header("Patrol")]
        public Transform[] patrolPoints;
        [Tooltip("Time the monster waits at each patrol point.")]
        public float waitTimeAtPoint = 1f;

        // ── Alert ────────────────────────────────────────────────────
        [Header("Alert")]
        [Tooltip("Time spent looking around at the alert position before returning to patrol.")]
        public float alertLookAroundTime = 4f;

        // ── Chase ─────────────────────────────────────────────────────
        [Header("Chase")]
        [Tooltip("Seconds without contact before switching to Search.")]
        public float loseTargetTime = 5f;
        [Tooltip("Time the monster stands at the last known position before switching to Search.")]
        public float lookAroundTime = 3f;

        // ── Search ────────────────────────────────────────────────────
        [Header("Search")]
        [Tooltip("Radius around last known position to pick random search waypoints.")]
        public float searchRadius = 8f;
        [Tooltip("Total search duration before giving up and returning to patrol.")]
        public float searchDuration = 10f;
        [Tooltip("Seconds between picking new random search waypoints.")]
        public float searchPointInterval = 3f;

        // ── Attack ────────────────────────────────────────────────────
        [Header("Attack")]
        [Tooltip("Distance at which the attack animation triggers.")]
        public float attackRange = 2f;
        [Tooltip("Distance at which a triggered attack actually connects.")]
        public float killDistance = 1.3f;
        [Tooltip("Total hits required to kill the player.")]
        [Range(1, 5)]
        public int hitsToKill = 2;
        [Tooltip("Apply physics knockback on the first hit.")]
        public bool useKnockback = true;
        [Tooltip("Force camera to look at the monster during the attack.")]
        public bool useCameraLock = true;
        [Tooltip("Seconds after attack start when the swing sound plays.")]
        public float attackSoundDelay = 0.3f;
        [Tooltip("Seconds after attack start when damage is applied (sync with animation).")]
        public float impactWaitTime = 0.5f;
        [Tooltip("How long the player's movement is locked on a non-fatal hit.")]
        public float playerLockDuration = 1.3f;
        [Tooltip("Extra seconds the monster stands still after unlocking the player (taunt window).")]
        public float monsterStunDuration = 3.5f;
        [Tooltip("Time the recovery timer lasts — player must stay out of sight to reset hits.")]
        public float damageRecoveryTime = 4f;
        [Tooltip("Knockback impulse force.")]
        public float knockbackForce = 8f;
        [Tooltip("Camera rotation-to-monster duration on attack.")]
        public float rotationTime = 0.4f;
        [Tooltip("Short pause before death screen appears after the final hit.")]
        public float deathVisibilityDuration = 0.01f;
        [Tooltip("Optional: specific transform the camera locks onto during attack (e.g. head bone).")]
        public Transform faceTarget;

        // ── Debug ─────────────────────────────────────────────────────
        [Header("Debug")]
        [Tooltip("Flag set externally when the player is hiding. Suppresses detection.")]
        public bool isPlayerHidden = false;
        [Tooltip("Show detection radii and patrol path in the Scene view.")]
        public bool showDebugGizmos = true;

        // ── State ──────────────────────────────────────────────────────
        public enum MonsterState { Patrol, Alert, Chase, Attack, Search, Scripted }
        [SerializeField] private MonsterState currentState = MonsterState.Patrol;

        // ── Private ────────────────────────────────────────────────────
        private NavMeshAgent navAgent;
        private MonsterAudio audioSystem;   // optional component; takes priority over inline clips
        private LightDetector lightDetector;
        private PlayerMovement playerMovement;
        private CameraController playerCamera;

        private int currentPatrolIndex = 0;
        private float waitTimer = 0f;

        private float alertLookTimer = 0f;
        private bool reachedAlertPosition = false;

        private float loseTargetTimer = 0f;
        private float lookAroundTimer = 0f;
        private bool reachedLastKnownPosition = false;

        private float searchTimer = 0f;
        private float searchPointTimer = 0f;

        private Vector3 lastKnownPlayerPosition;
        private bool playerInSight = false;

        private int currentHits = 0;
        private float recoveryTimer = 0f;
        private bool isEventActive = false;

        // Cached values
        private float visionRangeSqr;
        private float attackRangeSqr;
        private float closeDetectionRangeSqr;
        private float halfFOV;
        private Vector3 eyeOffset;

        // ─────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();
            navAgent.autoBraking = true;
            navAgent.updateRotation = true;
            navAgent.speed = patrolSpeed;
            navAgent.acceleration = navAcceleration;

            audioSystem = GetComponent<MonsterAudio>();
            lightDetector = GetComponent<LightDetector>();

            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
                playerCamera = player.GetComponentInChildren<CameraController>();
                if (sanityController == null)
                    sanityController = player.GetComponent<SanityController>();
            }

            if (faceTarget == null)
            {
                Transform head = transform.Find(
                    "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head");
                faceTarget = head != null ? head : transform;
            }

            if (damageOverlay != null)
            {
                damageOverlay.gameObject.SetActive(true);
                damageOverlay.color = Color.clear;
            }

            if (deathScreenPanel != null)
                deathScreenPanel.SetActive(false);

            if (voiceSource != null && patrolLoopSound != null)
            {
                voiceSource.clip = patrolLoopSound;
                voiceSource.loop = true;
                voiceSource.Play();
            }

            SoundManager.OnSoundEmitted += OnSoundHeard;

            if (patrolPoints != null && patrolPoints.Length > 0)
                GoToNextPatrolPoint();

            // Cache squared ranges for performance
            visionRangeSqr = visionRange * visionRange;
            attackRangeSqr = attackRange * attackRange;
            closeDetectionRangeSqr = closeDetectionRange * closeDetectionRange;
            halfFOV = fieldOfViewAngle * 0.5f;
            eyeOffset = Vector3.up * 1.6f;
        }

        void OnDestroy()
        {
            SoundManager.OnSoundEmitted -= OnSoundHeard;
        }

        void Update()
        {
            if (player == null || isEventActive) return;

            UpdateAnimations();

            // ── Hidden player branch ──────────────────────────────────
            if (isPlayerHidden)
            {
                playerInSight = false;

                if (currentState == MonsterState.Chase || currentState == MonsterState.Attack)
                    ChangeState(MonsterState.Search);

                // Reset hit state — player found a hiding spot
                if (currentHits > 0)
                {
                    currentHits = 0;
                    if (heartBeatAudio != null) heartBeatAudio.Stop();
                    if (sanityController != null) sanityController.SetHeartbeatMute(false);
                }

                FadeOutOverlay(2f);

                // Only non-aggressive states are allowed while hidden
                switch (currentState)
                {
                    case MonsterState.Patrol: PatrolBehavior(); break;
                    case MonsterState.Alert:  AlertBehavior();  break;
                    case MonsterState.Search: SearchBehavior(); break;
                    default: ChangeState(MonsterState.Search); break;
                }
                return;
            }

            // ── Scripted attack coroutine is running ──────────────────
            if (currentState == MonsterState.Scripted) return;

            // ── Damage recovery while player is out of sight ──────────
            HandleDamageRecovery();

            // ── Kill-distance check ───────────────────────────────────
            float dist = Vector3.Distance(transform.position, player.position);
            bool blocked = Physics.Linecast(
                transform.position + Vector3.up * 1.6f,
                player.position + Vector3.up,
                obstacleLayer);

            if (dist <= killDistance && !blocked)
            {
                StartCoroutine(AttackSequence());
                return;
            }

            // ── Detection ─────────────────────────────────────────────
            CheckDetection();

            // ── State machine ─────────────────────────────────────────
            switch (currentState)
            {
                case MonsterState.Patrol: PatrolBehavior(); break;
                case MonsterState.Alert:  AlertBehavior();  break;
                case MonsterState.Chase:  ChaseBehavior();  break;
                case MonsterState.Attack: AttackBehavior(); break;
                case MonsterState.Search: SearchBehavior(); break;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Detection

        void CheckDetection()
        {
            float distSqr = (player.position - transform.position).sqrMagnitude;

            // 1. Close range — guaranteed 360° detection (proximity sense)
            if (distSqr <= closeDetectionRangeSqr)
            {
                OnPlayerDetected();
                return;
            }

            // 2. Lantern / light detection (sighted monsters only)
            if (useLightDetection && lightDetector != null && lightDetector.IsLightDetected)
            {
                bool wallBetween = Physics.Linecast(
                    transform.position + Vector3.up * 1.5f,
                    player.position + Vector3.up,
                    obstacleLayer);

                if (!wallBetween)
                {
                    lastKnownPlayerPosition = lightDetector.LastLightPosition;
                    playerInSight = true;
                    if (currentState != MonsterState.Chase && currentState != MonsterState.Attack)
                        ChangeState(MonsterState.Chase);
                    return;
                }
            }

            // 3. FOV vision cone (disabled when visionRange == 0)
            if (visionRange > 0f && distSqr <= visionRangeSqr)
            {
                Vector3 eyePos = transform.position + eyeOffset;
                Vector3 dirToPlayer = player.position - eyePos;
                float angle = Vector3.Angle(transform.forward, dirToPlayer);

                if (angle <= halfFOV)
                {
                    float d = Mathf.Sqrt(distSqr);
                    Vector3 dirNorm = dirToPlayer / d;
                    RaycastHit hit;

                    if (Physics.Raycast(eyePos, dirNorm, out hit, d, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform == player || hit.transform.IsChildOf(player))
                        {
                            OnPlayerDetected();
                            return;
                        }
                    }
                }
            }

            playerInSight = false;
        }

        void OnPlayerDetected()
        {
            playerInSight = true;
            lastKnownPlayerPosition = player.position;
            loseTargetTimer = 0f;

            if (currentState == MonsterState.Patrol ||
                currentState == MonsterState.Alert  ||
                currentState == MonsterState.Search)
            {
                PlaySpotSound();
                ChangeState(MonsterState.Chase);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Hearing

        void OnSoundHeard(Vector3 soundPosition, float intensity, GameObject source)
        {
            if (source == gameObject || isPlayerHidden) return;
            if (currentState == MonsterState.Scripted) return;

            float distSqr = (soundPosition - transform.position).sqrMagnitude;
            float effectiveRange = hearingRange * intensity;

            if (distSqr > effectiveRange * effectiveRange) return;

            lastKnownPlayerPosition = soundPosition;

            if (currentState == MonsterState.Patrol ||
                currentState == MonsterState.Alert  ||
                currentState == MonsterState.Search)
            {
                ChangeState(MonsterState.Alert);
            }
            else if (currentState == MonsterState.Chase && !playerInSight)
            {
                // Update heading while chasing without sight
                loseTargetTimer = 0f;
                reachedLastKnownPosition = false;
                lookAroundTimer = 0f;
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region State Behaviors

        void PatrolBehavior()
        {
            navAgent.speed = patrolSpeed;

            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (waitTimer > 0f)
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
            // navAgent destination was already set in ChangeState
            if (!navAgent.pathPending && navAgent.remainingDistance < 1.5f)
            {
                if (!reachedAlertPosition)
                {
                    reachedAlertPosition = true;
                    alertLookTimer = 0f;
                }

                alertLookTimer += Time.deltaTime;
                if (alertLookTimer >= alertLookAroundTime)
                    ChangeState(MonsterState.Patrol);
            }
        }

        void ChaseBehavior()
        {
            if (playerInSight)
            {
                // Direct contact — keep updating destination
                lastKnownPlayerPosition = player.position;
                navAgent.SetDestination(lastKnownPlayerPosition);
                loseTargetTimer = 0f;
                lookAroundTimer = 0f;
                reachedLastKnownPosition = false;

                if ((player.position - transform.position).sqrMagnitude <= attackRangeSqr)
                    ChangeState(MonsterState.Attack);
            }
            else
            {
                // No contact — head to last known position
                navAgent.SetDestination(lastKnownPlayerPosition);

                if (!navAgent.pathPending && navAgent.remainingDistance < 1.5f)
                {
                    if (!reachedLastKnownPosition)
                    {
                        reachedLastKnownPosition = true;
                        lookAroundTimer = 0f;
                    }

                    lookAroundTimer += Time.deltaTime;
                    if (lookAroundTimer >= lookAroundTime)
                    {
                        ChangeState(MonsterState.Search);
                        return;
                    }
                }

                loseTargetTimer += Time.deltaTime;
                if (loseTargetTimer >= loseTargetTime)
                    ChangeState(MonsterState.Search);
            }
        }

        void AttackBehavior()
        {
            // Face the player while attack animates
            navAgent.SetDestination(transform.position);
            Vector3 flat = new Vector3(
                player.position.x - transform.position.x, 0f,
                player.position.z - transform.position.z);
            if (flat.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(flat), Time.deltaTime * 5f);

            float distSqr = (player.position - transform.position).sqrMagnitude;
            if (distSqr > attackRangeSqr || !playerInSight)
                ChangeState(MonsterState.Chase);
        }

        void SearchBehavior()
        {
            searchTimer += Time.deltaTime;
            searchPointTimer += Time.deltaTime;

            if (searchTimer >= searchDuration)
            {
                ChangeState(MonsterState.Patrol);
                return;
            }

            // Pick a new reachable point when timer fires or current destination is reached
            bool atDestination = !navAgent.pathPending && navAgent.remainingDistance < 1f;
            if (searchPointTimer >= searchPointInterval || atDestination)
            {
                searchPointTimer = 0f;
                Vector3 point = GetReachableSearchPoint(lastKnownPlayerPosition, searchRadius);
                navAgent.SetDestination(point);
            }
        }

        /// <summary>
        /// Picks a random NavMesh point around <paramref name="center"/> that the monster
        /// can actually reach (full path, no wall in the way). Falls back to the monster's
        /// current position if no valid point is found after <paramref name="maxAttempts"/> tries.
        /// </summary>
        Vector3 GetReachableSearchPoint(Vector3 center, float radius, int maxAttempts = 10)
        {
            NavMeshPath path = new NavMeshPath();

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

                NavMeshHit hit;
                // Use a tight snap radius so we don't land on the far side of a wall
                if (!NavMesh.SamplePosition(candidate, out hit, 2f, NavMesh.AllAreas))
                    continue;

                navAgent.CalculatePath(hit.position, path);
                if (path.status == NavMeshPathStatus.PathComplete)
                    return hit.position;
            }

            // No reachable point found — stay in place for this interval
            return transform.position;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region State Management

        void ChangeState(MonsterState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"[MonsterAI] {name}: {currentState} → {newState}");
            currentState = newState;

            switch (newState)
            {
                case MonsterState.Patrol:
                    navAgent.isStopped = false;
                    navAgent.updateRotation = true;
                    navAgent.speed = patrolSpeed;
                    navAgent.ResetPath();
                    loseTargetTimer = 0f;
                    lookAroundTimer = 0f;
                    reachedLastKnownPosition = false;
                    alertLookTimer = 0f;
                    reachedAlertPosition = false;
                    GoToNearestPatrolPoint();
                    break;

                case MonsterState.Alert:
                    alertLookTimer = 0f;
                    reachedAlertPosition = false;
                    navAgent.isStopped = false;
                    navAgent.speed = chaseSpeed;
                    navAgent.SetDestination(lastKnownPlayerPosition);
                    break;

                case MonsterState.Chase:
                    loseTargetTimer = 0f;
                    lookAroundTimer = 0f;
                    reachedLastKnownPosition = false;
                    navAgent.isStopped = false;
                    navAgent.speed = chaseSpeed;
                    break;

                case MonsterState.Search:
                    searchTimer = 0f;
                    searchPointTimer = searchPointInterval; // pick first waypoint immediately
                    lookAroundTimer = 0f;
                    reachedLastKnownPosition = false;
                    navAgent.speed = patrolSpeed;
                    navAgent.isStopped = false;
                    break;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Attack Sequence

        IEnumerator AttackSequence()
        {
            isEventActive = true;
            ChangeState(MonsterState.Scripted);

            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.updateRotation = false;

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.applyRootMotion = true;
                animator.SetTrigger("Attack");
            }

            if (sanityController != null) sanityController.SetHeartbeatMute(true);
            if (playerMovement != null)   playerMovement.SetMovementLock(true);
            if (useCameraLock && playerCamera != null)
                StartCoroutine(playerCamera.ForceLookAtRoutine(faceTarget, rotationTime));

            currentHits++;
            Debug.Log($"[MonsterAI] {name}: hit {currentHits}/{hitsToKill}");

            // Attack sound fires slightly before impact
            yield return new WaitForSeconds(attackSoundDelay);
            PlayAttackSound();

            yield return new WaitForSeconds(Mathf.Max(0f, impactWaitTime - attackSoundDelay));

            bool isFinalHit = currentHits >= hitsToKill;

            if (isFinalHit)
            {
                if (damageOverlay != null)
                {
                    damageOverlay.gameObject.SetActive(true);
                    damageOverlay.color = new Color(0.6f, 0f, 0f, 1f);
                }
                yield return HandleDeathSequence();
                yield break;
            }

            // ── Non-fatal hit ─────────────────────────────────────────
            if (damageOverlay != null)
            {
                damageOverlay.gameObject.SetActive(true);
                damageOverlay.color = new Color(0.8f, 0f, 0f, 0.3f);
            }

            if (heartBeatAudio != null) heartBeatAudio.Play();

            // Knockback
            if (useKnockback)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null && cc.enabled)
                {
                    Vector3 pushDir = (player.position - transform.position).normalized;
                    pushDir.y = 0f;
                    float elapsed = 0f;
                    while (elapsed < 0.2f)
                    {
                        elapsed += Time.deltaTime;
                        if (cc.enabled)
                            cc.Move(pushDir * knockbackForce * Time.deltaTime);
                        yield return null;
                    }
                }
            }

            recoveryTimer = damageRecoveryTime;

            // Unlock player after stun window
            yield return new WaitForSeconds(playerLockDuration);
            if (playerMovement != null) playerMovement.SetMovementLock(false);
            if (playerCamera != null)   playerCamera.SetInputLock(false);

            // Monster stands still (taunt window — player can run)
            navAgent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f);

            yield return new WaitForSeconds(monsterStunDuration);

            // Resume chase
            if (animator != null) animator.applyRootMotion = false;
            navAgent.updateRotation = true;
            navAgent.isStopped = false;

            isEventActive = false;
            ChangeState(MonsterState.Chase);
            navAgent.SetDestination(player.position);
        }

        IEnumerator HandleDeathSequence()
        {
            if (playerMovement != null) playerMovement.SetMovementLock(true);

            // Stop any active skill check
            var skillCheck = FindObjectOfType<SkillCheckSystem>();
            if (skillCheck != null) skillCheck.ForceStop();

            // Fade all game audio
            if (GlobalSoundManager.Instance != null)
                GlobalSoundManager.Instance.FadeOutAllSounds(1f);

            if (heartBeatAudio != null) heartBeatAudio.Stop();
            if (sanityController != null) sanityController.SetHeartbeatMute(true);
            if (voiceSource != null) voiceSource.Stop();
            if (feetSource != null)  feetSource.Stop();

            if (deathVisibilityDuration > 0f)
                yield return new WaitForSeconds(deathVisibilityDuration);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AudioListener.volume = 0f;

            if (deathScreenPanel != null)
            {
                CanvasGroup cg = deathScreenPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
                deathScreenPanel.SetActive(true);

                VideoPlayer vp = deathScreenPanel.GetComponentInChildren<VideoPlayer>();
                if (vp != null)
                {
                    vp.Prepare();
                    while (!vp.isPrepared) yield return null;
                    vp.Play();
                }

                if (cg != null) cg.alpha = 1f;
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Sends the monster to investigate a world position.
        /// Use for scripted events: generator explosion, triggered alarms, etc.
        /// </summary>
        public void TriggerChaseToLocation(Vector3 location)
        {
            if (currentState == MonsterState.Scripted) return;

            lastKnownPlayerPosition = location;
            loseTargetTimer = 0f;
            PlaySpotSound();
            ChangeState(MonsterState.Chase);
        }

        /// <summary>
        /// Called from animation events to play a footstep sound.
        /// </summary>
        public void PlayStepSound()
        {
            if (feetSource == null || stepSound == null) return;
            if (navAgent.velocity.magnitude < 0.1f) return;

            feetSource.pitch = Mathf.Lerp(0.9f, 1.15f, navAgent.velocity.magnitude / chaseSpeed)
                               + Random.Range(-0.05f, 0.05f);
            feetSource.PlayOneShot(stepSound);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Helpers

        void HandleDamageRecovery()
        {
            if (currentHits != 1) return;

            if (playerInSight)
            {
                recoveryTimer = damageRecoveryTime;
            }
            else
            {
                recoveryTimer -= Time.deltaTime;
                if (recoveryTimer <= 0f)
                {
                    currentHits = 0;
                    if (heartBeatAudio != null) heartBeatAudio.Stop();
                    if (sanityController != null) sanityController.SetHeartbeatMute(false);
                }
            }

            // Overlay fades proportionally as recovery timer counts down
            if (damageOverlay != null)
            {
                float alpha = Mathf.Lerp(0f, 0.5f, recoveryTimer / damageRecoveryTime);
                damageOverlay.color = new Color(1f, 0f, 0f, alpha);
            }
        }

        void FadeOutOverlay(float speed)
        {
            if (damageOverlay == null || damageOverlay.color.a <= 0.01f) return;
            damageOverlay.color = Color.Lerp(damageOverlay.color, Color.clear, Time.deltaTime * speed);
        }

        void UpdateAnimations()
        {
            if (animator == null) return;
            float speed = navAgent.isStopped ? 0f : navAgent.velocity.magnitude;
            animator.SetFloat("Speed", speed, 0.05f, Time.deltaTime);
        }

        void PlaySpotSound()
        {
            if (audioSystem != null) { audioSystem.PlaySpotSound(); return; }
            if (voiceSource != null && spotSound != null) voiceSource.PlayOneShot(spotSound);
        }

        void PlayAttackSound()
        {
            if (audioSystem != null) { audioSystem.PlayScream(); return; }
            if (voiceSource != null && attackSound != null) voiceSource.PlayOneShot(attackSound);
        }

        void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        void GoToNearestPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            int nearest = 0;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float sqr = (patrolPoints[i].position - transform.position).sqrMagnitude;
                if (sqr < nearestSqr) { nearestSqr = sqr; nearest = i; }
            }

            currentPatrolIndex = nearest;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Debug Gizmos

        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Hearing range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            // Close detection / proximity
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, closeDetectionRange);

            // FOV vision cone (only when enabled)
            if (visionRange > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, visionRange);

                Vector3 fwd = transform.forward * visionRange;
                Gizmos.color = playerInSight ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position,
                    transform.position + Quaternion.Euler(0f,  halfFOV, 0f) * fwd);
                Gizmos.DrawLine(transform.position,
                    transform.position + Quaternion.Euler(0f, -halfFOV, 0f) * fwd);
            }

            // Attack & kill range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, killDistance);

            // Patrol path
            if (patrolPoints != null && patrolPoints.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                    int next = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[next] != null)
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
                }
            }

            // Last known player position
            if (currentState != MonsterState.Patrol)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1f);
            }
        }

        #endregion
    }
}