using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        [Header("Speed Settings (Швидкість бігунка)")]
        [SerializeField] private float startRotationSpeed = 250f;
        [SerializeField] private float speedIncreaseOnHit = 20f;
        private float rotationSpeed;

        [Header("Zone Settings (Математика Зон)")]
        [Range(0.05f, 0.5f)]
        [Tooltip("Відсоток ширини бару для Успішної зони (напр. 0.2 = 20%)")]
        public float successZoneSize = 0.2f; 
        
        [Range(0.01f, 0.2f)]
        [Tooltip("Відсоток ширини бару для Ідеальної зони")]
        public float perfectZoneSize = 0.05f;

        [Tooltip("ХІТРОЩІ: Прихований допуск для гравця. Робить зону влучання трішки ширшою за візуальну.")]
        [SerializeField] private float forgivenessMargin = 0.015f; 

        [SerializeField] private float randomPositionRadius = 200f;
        [SerializeField] private float minSpawnDistance = 150f; 
        private Vector2 lastPosition = Vector2.zero;

        [Header("Zone Appearance")]
        [SerializeField] private Color successZoneColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color perfectZoneColor = new Color(1f, 0.85f, 0f, 0.95f);
        
        [Header("Visual Feedback")]
        [Tooltip("Скільки секунд голка стоїть на місці після кліку, щоб гравець побачив своє влучання/промах")]
        [SerializeField] private float pauseAfterHit = 0.2f;

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
        private bool isPaused = false;
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

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            if (!isActive || isPaused) return;

            currentRotation += needleDirection * (rotationSpeed / barWidth) * Time.deltaTime;
            
            if (currentRotation >= 1f) { currentRotation = 1f; needleDirection = -1f; }
            if (currentRotation <= 0f) { currentRotation = 0f; needleDirection = 1f; }

            if (needlePivot)
            {
                RectTransform nr = needlePivot as RectTransform ?? needlePivot.GetComponent<RectTransform>();
                if (nr != null)
                {
                    float xPos = Mathf.Lerp(-barWidth / 2f, barWidth / 2f, currentRotation);
                    nr.anchoredPosition = new Vector2(xPos, 0f);
                    nr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                CheckHit();
            }
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
            StopAllCoroutines();
            if (skillCheckContainer) skillCheckContainer.gameObject.SetActive(false);
            if (progressSlider) progressSlider.gameObject.SetActive(false);
            if (spacePromptText) spacePromptText.gameObject.SetActive(false);
        }

        private void StartRound()
        {
            isActive = true;
            isPaused = false;

            if (skillCheckContainer)
            {
                skillCheckContainer.gameObject.SetActive(true);

                Vector2 newPos;
                int attempts = 0;
                do {
                    newPos = Random.insideUnitCircle * randomPositionRadius;
                    attempts++;
                } while (Vector2.Distance(newPos, lastPosition) < minSpawnDistance && attempts < 10);
                skillCheckContainer.anchoredPosition = newPos;
                lastPosition = newPos;
            }
            if (spacePromptText) spacePromptText.gameObject.SetActive(true);

            float safeMargin = successZoneSize / 2f; 

            targetZoneAngle = Random.Range(safeMargin, 1f - safeMargin); 

            float zoneCenterPixel = Mathf.Lerp(-barWidth / 2f, barWidth / 2f, targetZoneAngle);

            if (successZoneImage)
            {
                successZoneImage.color = successZoneColor;
                RectTransform sr = successZoneImage.GetComponent<RectTransform>();
                if (sr != null)
                {
                    sr.anchoredPosition = new Vector2(zoneCenterPixel, 0f);
                    sr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, successZoneSize * barWidth);
                    sr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            if (perfectZoneImage)
            {
                perfectZoneImage.color = perfectZoneColor;
                RectTransform pr = perfectZoneImage.GetComponent<RectTransform>();
                if (pr != null)
                {
                    pr.anchoredPosition = new Vector2(zoneCenterPixel, 0f);
                    pr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, perfectZoneSize * barWidth);
                    pr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
                }
            }

            currentRotation = 0f;
            needleDirection = 1f;

            if (audioSource && appearSound) audioSource.PlayOneShot(appearSound);
        }

        private void CheckHit()
        {
            isPaused = true; 

            float posDiff = Mathf.Abs(currentRotation - targetZoneAngle);
            
            float successThreshold = (successZoneSize / 2f) + forgivenessMargin;
            float perfectThreshold = (perfectZoneSize / 2f) + forgivenessMargin;

            bool hitPerfect = posDiff <= perfectThreshold;
            bool hitGood = posDiff <= successThreshold;

            if (hitGood)
            {
                consecutiveMisses = 0;
                currentProgress += hitPerfect ? perfectBonus : successBonus;
                if (currentProgress > 100f) currentProgress = 100f;

                if (audioSource && successSound) audioSource.PlayOneShot(successSound);
                UpdateUI();

                if (currentProgress >= 100f) 
                    StartCoroutine(HitPauseRoutine(true));
                else 
                {
                    rotationSpeed += speedIncreaseOnHit; 
                    StartCoroutine(HitPauseRoutine(false));
                }
            }
            else
            {
                consecutiveMisses++;
                currentProgress -= failPenalty;
                if (currentProgress < 0) currentProgress = 0;

                if (audioSource && failSound) audioSource.PlayOneShot(failSound);
                UpdateUI();

                if (consecutiveMisses >= maxConsecutiveMisses) 
                {
                    FailSequence();
                }
                else 
                {
                    StartCoroutine(HitPauseRoutine(false));
                }
            }
        }

        private IEnumerator HitPauseRoutine(bool isComplete)
        {
            yield return new WaitForSeconds(pauseAfterHit); 

            if (isComplete)
            {
                Complete();
            }
            else
            {
                StartRound();
            }
        }

        private void UpdateUI()
        {
            if (progressSlider) progressSlider.value = currentProgress / 100f;
            if (percentTextTMP != null) percentTextTMP.text = $"{Mathf.RoundToInt(currentProgress)}%";
        }

        private void FailSequence()
        {
            ForceStop();
            if (audioSource && alarmSound) audioSource.PlayOneShot(alarmSound);
            OnFail?.Invoke();
        }

        private void Complete()
        {
            ForceStop();
            if (audioSource && completeSound) audioSource.PlayOneShot(completeSound);
            OnSeriesComplete?.Invoke();
        }
    }
}