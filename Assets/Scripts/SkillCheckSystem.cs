using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    public class SkillCheckSystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform skillCheckContainer;
        [SerializeField] private RectTransform barContainer;
        [SerializeField] private Transform needlePivot;
        [SerializeField] private Image successZoneImage;
        [SerializeField] private Image perfectZoneImage;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI percentTextTMP;
        [SerializeField] private TextMeshProUGUI spacePromptText;

        [Header("Speed Settings (Налаштування швидкості бігунка голки)")]
        [Tooltip("Початкова швидкість бігунка (пікселів за секунду)")]
        [SerializeField] private float startRotationSpeed = 250f;

        [Tooltip("На скільки прискорюється бігунок після кожного вдалого влучання")]
        [SerializeField] private float speedIncreaseOnHit = 20f;
        private float rotationSpeed;

        [Header("Settings")]

        [Range(0.05f, 0.5f)]
        public float successZoneSize = 0.15f;
        [SerializeField] private float randomPositionRadius = 200f;

        [Tooltip("Наскільки далеко має стрибнути наступний скілчек від попереднього")]
        [SerializeField] private float minSpawnDistance = 150f; 
        private Vector2 lastPosition = Vector2.zero;

        [Tooltip("Розмір одного сегменту бару (20 = один квадратик)")]
        [SerializeField] private float tileSize = 20f;

        [Header("Zone Appearance")]
        [SerializeField] private Color successZoneColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color perfectZoneColor = new Color(1f, 0.85f, 0f, 0.95f);

        [Header("Progress Settings")]
        public float successBonus = 25f;
        public float perfectBonus = 20f;
        public float failPenalty = 15f;
        public int maxConsecutiveMisses = 3;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failSound;
        [SerializeField] private AudioClip completeSound;
        [SerializeField] private AudioClip appearSound;
        [SerializeField] private AudioClip alarmSound;
        public System.Action OnSeriesComplete;
        public System.Action OnFail;
        private bool isActive = false;
        private float currentRotation = 0f;
        private float targetZoneAngle = 0f;
        private float needleDirection = 1f;
        private float barWidth = 300f;
        private float barHeight = 30f;
        private float currentProgress = 0f;
        private int consecutiveMisses = 0;
        private void Start()
        {
            if (barContainer != null)
            {
                barWidth = barContainer.rect.width;
                barHeight = barContainer.rect.height;
            }

            if (skillCheckContainer) skillCheckContainer.gameObject.SetActive(false);
            if (progressSlider) progressSlider.gameObject.SetActive(false);
            if (spacePromptText) spacePromptText.gameObject.SetActive(false);

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        private void Update()
        {
            if (!isActive) return;

            currentRotation += needleDirection * (rotationSpeed / barWidth) * Time.deltaTime;
            if (currentRotation >= 1f) { currentRotation = 1f; needleDirection = -1f; }
            if (currentRotation <= 0f) { currentRotation = 0f; needleDirection = 1f; }

            if (needlePivot)
            {
                RectTransform nr = needlePivot as RectTransform ?? needlePivot.GetComponent<RectTransform>();
                if (nr != null)
                {
                    float xPos = Mathf.Clamp(
                        (currentRotation - 0.5f) * barWidth,
                        -barWidth / 2f,
                        barWidth / 2f
                    );
                    nr.anchoredPosition = new Vector2(xPos, 0f);
                    nr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
                CheckHit();
        }
        public void StartRepair()
        {
            currentProgress = 0f;
            consecutiveMisses = 0;
            rotationSpeed = startRotationSpeed;

            if (barContainer != null)
            {
                barWidth = barContainer.rect.width;
                barHeight = barContainer.rect.height;
            }

            if (progressSlider) { progressSlider.gameObject.SetActive(true); progressSlider.value = 0; }
            UpdateUI();
            StartRound();
        }
        public void ForceStop()
        {
            isActive = false;
            if (skillCheckContainer) skillCheckContainer.gameObject.SetActive(false);
            if (progressSlider) progressSlider.gameObject.SetActive(false);
            if (spacePromptText) spacePromptText.gameObject.SetActive(false);
        }
        private void StartRound()
        {
            isActive = true;
            if (skillCheckContainer)
            {
                skillCheckContainer.gameObject.SetActive(true);

                Vector2 newPos;
                int attempts = 0;

                do
                {
                    newPos = Random.insideUnitCircle * randomPositionRadius;
                    
                    attempts++;
                } 
                while (Vector2.Distance(newPos, lastPosition) < minSpawnDistance && attempts < 10);

                skillCheckContainer.anchoredPosition = newPos;

                lastPosition = newPos;
            }
            if (spacePromptText) spacePromptText.gameObject.SetActive(true);

            int totalTiles = Mathf.RoundToInt(barWidth / tileSize);

            int successTiles = Mathf.Max(1, Mathf.RoundToInt(successZoneSize * totalTiles));
            float successWidth = successTiles * tileSize;

            int minTile = successTiles / 2 + 1;
            int maxTile = totalTiles - successTiles / 2 - 1;
            int randomTile = Random.Range(minTile, maxTile + 1);
            targetZoneAngle = (float)randomTile / totalTiles;

            float zoneX = (targetZoneAngle - 0.5f) * barWidth;

            if (successZoneImage)
            {
                successZoneImage.color = successZoneColor;
                RectTransform sr = successZoneImage.GetComponent<RectTransform>();
                if (sr != null)
                {
                    sr.anchoredPosition = new Vector2(zoneX, 0f);
                    sr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, successWidth);
                    sr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            if (perfectZoneImage)
            {
                perfectZoneImage.color = perfectZoneColor;
                RectTransform pr = perfectZoneImage.GetComponent<RectTransform>();
                if (pr != null)
                {
                    pr.anchoredPosition = new Vector2(zoneX, 0f);
                    pr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tileSize);
                    pr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            currentRotation = 0f;
            needleDirection = 1f;
            if (needlePivot)
            {
                RectTransform nr = needlePivot as RectTransform ?? needlePivot.GetComponent<RectTransform>();
                if (nr != null)
                {
                    nr.anchoredPosition = new Vector2(-barWidth / 2f, 0f);
                    nr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            if (audioSource && appearSound) audioSource.PlayOneShot(appearSound);
        }
        private void CheckHit()
        {
            int totalTiles = Mathf.RoundToInt(barWidth / tileSize);
            int successTiles = Mathf.Max(1, Mathf.RoundToInt(successZoneSize * totalTiles));

            float posDiff = Mathf.Abs(currentRotation - targetZoneAngle);
            float successHalfNorm = (successTiles * tileSize / 2f) / barWidth;
            float perfectHalfNorm = (tileSize / 2f) / barWidth;

            bool hitPerfect = posDiff <= perfectHalfNorm;
            bool hitGood = posDiff <= successHalfNorm;

            if (hitGood)
            {
                consecutiveMisses = 0;
                currentProgress += hitPerfect ? perfectBonus : successBonus;
                if (currentProgress > 100f) currentProgress = 100f;

                Debug.Log(hitPerfect ? "Perfect! +20%" : "Good! +25%");
                if (audioSource && successSound) audioSource.PlayOneShot(successSound);
                UpdateUI();

                if (currentProgress >= 100f) Complete();
                else 
                {
                    rotationSpeed += speedIncreaseOnHit; 
                    StartRound(); 
                }
            }
            else
            {
                consecutiveMisses++;
                currentProgress -= failPenalty;
                if (currentProgress < 0) currentProgress = 0;

                Debug.Log($"Miss! {consecutiveMisses}/{maxConsecutiveMisses}");
                if (audioSource && failSound) audioSource.PlayOneShot(failSound);
                UpdateUI();

                if (consecutiveMisses >= maxConsecutiveMisses) FailSequence();
                else StartRound();
            }
        }
        private void UpdateUI()
        {
            if (progressSlider) progressSlider.value = currentProgress / 100f;
            if (percentTextTMP != null) percentTextTMP.text = $"{Mathf.RoundToInt(currentProgress)}%";
        }
        private void FailSequence()
        {
            isActive = false;
            if (skillCheckContainer) skillCheckContainer.gameObject.SetActive(false);
            if (progressSlider) progressSlider.gameObject.SetActive(false);
            if (spacePromptText) spacePromptText.gameObject.SetActive(false);
            if (audioSource && alarmSound) audioSource.PlayOneShot(alarmSound);
            OnFail?.Invoke();
        }
        private void Complete()
        {
            isActive = false;
            if (skillCheckContainer) skillCheckContainer.gameObject.SetActive(false);
            if (progressSlider) progressSlider.gameObject.SetActive(false);
            if (spacePromptText) spacePromptText.gameObject.SetActive(false);
            if (audioSource && completeSound) audioSource.PlayOneShot(completeSound);
            OnSeriesComplete?.Invoke();
        }
    }
}