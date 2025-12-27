using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace Assets.Scripts
{
    public class DoorTaskManager : MonoBehaviour
    {
        [Header("Door 1 UI (Green/Yellow)")]
        [SerializeField] private GameObject door1Panel; 
        
        [SerializeField] private TextMeshProUGUI txtGreenKey;
        [SerializeField] private Image iconGreenKey;
        [SerializeField] private GameObject lineGreenKey;

        [SerializeField] private TextMeshProUGUI txtYellowKey;
        [SerializeField] private Image iconYellowKey;
        [SerializeField] private GameObject lineYellowKey;

        [SerializeField] private GameObject strikeThroughLine1;

        [Header("Door 2 UI (Blue/Pink)")]
        [SerializeField] private GameObject door2Panel;
        
        [SerializeField] private TextMeshProUGUI txtBlueKey;
        [SerializeField] private Image iconBlueKey;
        [SerializeField] private GameObject lineBlueKey;

        [SerializeField] private TextMeshProUGUI txtPinkKey;
        [SerializeField] private Image iconPinkKey;
        [SerializeField] private GameObject linePinkKey;

        [SerializeField] private GameObject strikeThroughLine2;

        [Header("Settings")]
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color activeColor = Color.white;
        
        [Header("Animation Settings")]
        [Tooltip("Скільки часу чекати перед початком зникнення (щоб гравець встиг прочитати)")]
        [SerializeField] private float delayBeforeHide = 2.0f;
        [Tooltip("Як довго триває саме плавне зникнення")]
        [SerializeField] private float fadeDuration = 2.0f;

        private SimpleInventory inventory;
        private bool isDoor1Finished = false; 
        private bool isDoor2Finished = false;

        private string originalTxtGreen, originalTxtYellow, originalTxtBlue, originalTxtPink;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) inventory = player.GetComponent<SimpleInventory>();

            if (txtGreenKey) originalTxtGreen = txtGreenKey.text;
            if (txtYellowKey) originalTxtYellow = txtYellowKey.text;
            if (txtBlueKey) originalTxtBlue = txtBlueKey.text;
            if (txtPinkKey) originalTxtPink = txtPinkKey.text;

            if (door1Panel) door1Panel.SetActive(false);
            if (door2Panel) door2Panel.SetActive(false);

            if (lineGreenKey) lineGreenKey.SetActive(false);
            if (lineYellowKey) lineYellowKey.SetActive(false);
            if (lineBlueKey) lineBlueKey.SetActive(false);
            if (linePinkKey) linePinkKey.SetActive(false);

            SimpleInventory.OnInventoryChanged += UpdateUI;
        }

        private void OnDestroy()
        {
            SimpleInventory.OnInventoryChanged -= UpdateUI;
        }

        private void UpdateUI()
        {
            if (inventory == null) return;

            // --- DOOR 1 LOGIC ---
            if (!isDoor1Finished)
            {
                bool hasGreen = inventory.HasKey(KeyColorType.Green);
                bool hasYellow = inventory.HasKey(KeyColorType.Yellow);

                if (hasGreen || hasYellow) 
                {
                    ShowPanel(door1Panel);
                }

                UpdateSingleTask(txtGreenKey, iconGreenKey, lineGreenKey, hasGreen, originalTxtGreen);
                UpdateSingleTask(txtYellowKey, iconYellowKey, lineYellowKey, hasYellow, originalTxtYellow);

                if (hasGreen && hasYellow)
                {
                    isDoor1Finished = true;
                    if (strikeThroughLine1 != null) strikeThroughLine1.SetActive(true);

                    StartCoroutine(HidePanelRoutine(door1Panel));
                }
            }

            // --- DOOR 2 LOGIC ---
            if (!isDoor2Finished)
            {
                bool hasBlue = inventory.HasKey(KeyColorType.Blue);
                bool hasPink = inventory.HasKey(KeyColorType.Pink);

                if (hasBlue || hasPink)
                {
                    ShowPanel(door2Panel);
                }

                UpdateSingleTask(txtBlueKey, iconBlueKey, lineBlueKey, hasBlue, originalTxtBlue);
                UpdateSingleTask(txtPinkKey, iconPinkKey, linePinkKey, hasPink, originalTxtPink);

                if (hasBlue && hasPink)
                {
                    isDoor2Finished = true;
                    if (strikeThroughLine2 != null) strikeThroughLine2.SetActive(true);
                    
                    StartCoroutine(HidePanelRoutine(door2Panel));
                }
            }
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel != null && !panel.activeSelf)
            {
                panel.SetActive(true);

                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        private void UpdateSingleTask(TextMeshProUGUI textComp, Image icon, GameObject lineObj, bool isCollected, string originalText)
        {
            if (textComp != null)
            {
                textComp.text = originalText;

                if (isCollected)
                {
                    textComp.color = activeColor;
                    if (lineObj != null) lineObj.SetActive(true);
                }
                else
                {
                    textComp.color = inactiveColor;
                    if (lineObj != null) lineObj.SetActive(false);
                }
            }
            
            if (icon != null)
            {
                icon.color = isCollected ? Color.white : new Color(1, 1, 1, 0.3f);
            }
        }

        private IEnumerator HidePanelRoutine(GameObject panel)
        {
            yield return new WaitForSeconds(delayBeforeHide);

            if (panel != null)
            {
                CanvasGroup cg = panel.GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    float time = 0f;
                    float startAlpha = cg.alpha;

                    while (time < fadeDuration)
                    {
                        time += Time.deltaTime;

                        cg.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
                        yield return null;
                    }
                    cg.alpha = 0f;
                }

                panel.SetActive(false);
            }
        }
    }
}