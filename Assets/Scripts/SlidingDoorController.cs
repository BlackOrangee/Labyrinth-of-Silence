using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts
{
    /// <summary>
    /// Sliding door controller (single or double panels).
    /// Requires key(s) to open. Shows ThoughtTrigger hint when locked instead of a popup.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SlidingDoorController : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        public class DoorPanel
        {
            [Tooltip("Door panel transform (collider object)")]
            public Transform panel;
            [Tooltip("Local position offset when fully open")]
            public Vector3 openOffset;
            [Tooltip("Extra transforms to move with the same offset (mesh objects, etc.)")]
            public List<Transform> extraTransforms;

            [Header("Broken Door")]
            [Tooltip("How far the panel actually opens (1 = full, 0.5 = halfway, 0 = stuck)")]
            [Range(0f, 1f)]
            public float openFraction = 1f;
            [Tooltip("Stutter/jam effect — door hesitates before stopping at broken position")]
            public bool broken = false;
            [Tooltip("Extra delay before this panel starts moving (for async feel)")]
            public float panelDelay = 0f;

            [Tooltip("Min/Max number of stops during broken movement")]
            public Vector2Int stopCountRange = new Vector2Int(3, 5);
            [Tooltip("Min/Max pause duration at each stop (seconds)")]
            public Vector2 pauseRange = new Vector2(0.15f, 0.7f);
            [Tooltip("Min/Max movement speed multiplier between stops")]
            public Vector2 speedRange = new Vector2(0.8f, 1.4f);
            [Tooltip("Min/Max speed multiplier for the final move to jammed position")]
            public Vector2 finalSpeedRange = new Vector2(0.4f, 0.8f);
        }

        [Header("Door Identity")]
        [SerializeField] private string doorName = "Sliding Door";

        [Header("Panels")]
        [Tooltip("Add one panel for single door, two for double sliding doors")]
        [SerializeField] private List<DoorPanel> panels = new List<DoorPanel>();

        [Header("Key Requirements")]
        [Tooltip("Leave empty to allow opening without a key")]
        [SerializeField] private List<KeyColorType> requiredKeys = new List<KeyColorType>();

        [Header("Settings")]
        [SerializeField] private float openSpeed = 2f;
        [Tooltip("Delay before panels start moving (seconds). Sound plays immediately.")]
        [SerializeField] private float openDelay = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;

        [Header("Locked Thought")]
        [Tooltip("If ThoughtTrigger is attached — it fires when door is locked instead of a popup.\n" +
                 "Set ThoughtTrigger mode to OnInteraction.")]
        [SerializeField] private bool showThoughtWhenLocked = true;

        [Header("Task System")]
        [SerializeField] private string taskIDToRemove;
        [SerializeField] private string taskIDToAdd;
        [SerializeField] private string taskTextEng;
        [SerializeField] private string taskTextUkr;
        [Tooltip("Quest already given — set automatically, do not check manually")]
        [SerializeField] private bool taskGiven = false;

        private bool isOpen = false;
        private SimpleInventory playerInventory;
        private AudioSource audioSource;
        private Vector3[] closedPositions;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            foreach (var anim in GetComponentsInChildren<Animator>(true))
            {
                Debug.Log($"[SlidingDoorController] Found and destroyed Animator on '{anim.gameObject.name}'");
                Destroy(anim);
            }

            closedPositions = new Vector3[panels.Count];
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panel != null)
                    closedPositions[i] = panels[i].panel.localPosition;
            }

            var player = GameObject.FindWithTag("Player");
            if (player != null)
                playerInventory = player.GetComponent<SimpleInventory>();
        }

        public void Interact(GameObject actor)
        {
            if (isOpen) return;

            if (playerInventory == null)
                playerInventory = actor.GetComponent<SimpleInventory>();

            bool hasKeys = requiredKeys.Count == 0 ||
                           (playerInventory != null && playerInventory.HasAllKeys(requiredKeys));

            if (hasKeys)
            {
                OpenDoor();
            }
            else
            {
                if (showThoughtWhenLocked)
                    TriggerLockedThought();

                if (!taskGiven && TaskManager.Instance != null)
                {
                    taskGiven = true;

                    if (!string.IsNullOrEmpty(taskIDToRemove))
                        TaskManager.Instance.RemoveTask(taskIDToRemove);

                    if (!string.IsNullOrEmpty(taskIDToAdd) && !string.IsNullOrEmpty(taskTextEng))
                        TaskManager.Instance.AddTask(taskIDToAdd, taskTextEng, taskTextUkr);
                }
            }
        }

        public void OnInteract(GameObject actor) => Interact(actor);

        public string GetPopupID() => isOpen ? "" : "open";

        private void OpenDoor()
        {
            isOpen = true;
            PlaySound(openSound);
            StartCoroutine(OpenDoorRoutine());
        }

        private IEnumerator OpenDoorRoutine()
        {
            if (openDelay > 0f)
                yield return new WaitForSeconds(openDelay);

            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                if (p.panel == null) continue;

                Vector3 target = closedPositions[i] + p.openOffset * p.openFraction;

                if (p.broken)
                    StartCoroutine(SlideBrokenRoutine(p, target));
                else
                    StartCoroutine(SlideAllTransforms(p, target));
            }

            if (!taskGiven && TaskManager.Instance != null)
            {
                taskGiven = true;

                if (!string.IsNullOrEmpty(taskIDToRemove))
                    TaskManager.Instance.RemoveTask(taskIDToRemove);

                if (!string.IsNullOrEmpty(taskIDToAdd) && !string.IsNullOrEmpty(taskTextEng))
                    TaskManager.Instance.AddTask(taskIDToAdd, taskTextEng, taskTextUkr);
            }
        }

        /// <summary>
        /// Normal smooth slide for all transforms of a panel.
        /// </summary>
        private IEnumerator SlideAllTransforms(DoorPanel p, Vector3 target)
        {
            if (p.panelDelay > 0f)
                yield return new WaitForSeconds(p.panelDelay);

            yield return SlidePanelRoutine(p.panel, target, openSpeed);

            if (p.extraTransforms == null) yield break;
            foreach (var extra in p.extraTransforms)
            {
                if (extra != null)
                    StartCoroutine(SlidePanelRoutine(extra, extra.localPosition + p.openOffset * p.openFraction, openSpeed));
            }
        }

        /// <summary>
        /// Broken/jammed slide: door moves in bursts with random speed variation, then jams.
        /// </summary>
        private IEnumerator SlideBrokenRoutine(DoorPanel p, Vector3 target)
        {
            if (p.panelDelay > 0f)
                yield return new WaitForSeconds(p.panelDelay);

            Vector3 startPos = p.panel.localPosition;

            int stopCount = Random.Range(p.stopCountRange.x, p.stopCountRange.y + 1);
            float[] stopPoints = new float[stopCount];
            for (int s = 0; s < stopCount; s++)
                stopPoints[s] = (s + 1f) / (stopCount + 1f) + Random.Range(-0.08f, 0.08f);
            System.Array.Sort(stopPoints);

            for (int s = 0; s < stopCount; s++)
            {
                float t = Mathf.Clamp01(stopPoints[s]);
                Vector3 stopPos = Vector3.Lerp(startPos, target, t);

                yield return SlidePanelRoutine(p.panel, stopPos, openSpeed * Random.Range(p.speedRange.x, p.speedRange.y));
                SyncExtras(p, stopPos - startPos);

                yield return new WaitForSeconds(Random.Range(p.pauseRange.x, p.pauseRange.y));
            }

            yield return SlidePanelRoutine(p.panel, target, openSpeed * Random.Range(p.finalSpeedRange.x, p.finalSpeedRange.y));
            SyncExtras(p, target - startPos);
        }

        private void SyncExtras(DoorPanel p, Vector3 appliedOffset)
        {
            if (p.extraTransforms == null) return;
            foreach (var extra in p.extraTransforms)
            {
                if (extra != null)
                    extra.localPosition = closedPositions[panels.IndexOf(p)] + appliedOffset;
            }
        }

        private IEnumerator SlidePanelRoutine(Transform panel, Vector3 targetLocalPos, float speed)
        {
            Vector3 startPos = panel.localPosition;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                panel.localPosition = Vector3.Lerp(startPos, targetLocalPos, t);
                yield return null;
            }

            panel.localPosition = targetLocalPos;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void TriggerLockedThought()
        {
            ThoughtTrigger thought = GetComponent<ThoughtTrigger>();
            if (thought != null)
                thought.TriggerFromInteraction();
        }

        public bool IsOpen() => isOpen;

        [ContextMenu("Test Open Door")]
        private void TestOpen()
        {
            isOpen = false;
            OpenDoor();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isOpen ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);

            foreach (var p in panels)
            {
                if (p.panel == null) continue;
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Vector3 openWorldPos = p.panel.parent != null
                    ? p.panel.parent.TransformPoint(p.panel.localPosition + p.openOffset * p.openFraction)
                    : p.panel.position + p.openOffset * p.openFraction;
                Gizmos.DrawLine(p.panel.position, openWorldPos);
                Gizmos.DrawWireCube(openWorldPos, Vector3.one * 0.2f);
            }
        }
    }
}