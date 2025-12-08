using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace Assets.Scripts
{
    public class DoorTaskManager : MonoBehaviour
    {
        [Header("Door 1 UI (Green/Yellow)")]
        public GameObject door1Panel; 
        
        public TextMeshProUGUI txtGreenKey;
        public Image iconGreenKey;
        public GameObject lineGreenKey;

        public TextMeshProUGUI txtYellowKey;
        public Image iconYellowKey;
        public GameObject lineYellowKey;

        public GameObject strikeThroughLine1;

        [Header("Door 2 UI (Blue/Pink)")]
        public GameObject door2Panel;
        
        public TextMeshProUGUI txtBlueKey;
        public Image iconBlueKey;
        public GameObject lineBlueKey;

        public TextMeshProUGUI txtPinkKey;
        public Image iconPinkKey;
        public GameObject linePinkKey;

        public GameObject strikeThroughLine2;

        [Header("Settings")]
        public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color activeColor = Color.white;
        public float delayBeforeHide = 2.0f;

        private SimpleInventory inventory;
        private bool isDoor1Finished = false; 
        private bool isDoor2Finished = false;

        private string originalTxtGreen, originalTxtYellow, originalTxtBlue, originalTxtPink;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) inventory = player.GetComponent<SimpleInventory>();

            if(txtGreenKey) originalTxtGreen = txtGreenKey.text;
            if(txtYellowKey) originalTxtYellow = txtYellowKey.text;
            if(txtBlueKey) originalTxtBlue = txtBlueKey.text;
            if(txtPinkKey) originalTxtPink = txtPinkKey.text;

            if(door1Panel) door1Panel.SetActive(false);
            if(door2Panel) door2Panel.SetActive(false);

            if(lineGreenKey) lineGreenKey.SetActive(false);
            if(lineYellowKey) lineYellowKey.SetActive(false);
            if(lineBlueKey) lineBlueKey.SetActive(false);
            if(linePinkKey) linePinkKey.SetActive(false);

            SimpleInventory.OnInventoryChanged += UpdateUI;
        }

        private void OnDestroy()
        {
            SimpleInventory.OnInventoryChanged -= UpdateUI;
        }

        private void UpdateUI()
        {
            if (inventory == null) return;

            if (!isDoor1Finished)
            {
                bool hasGreen = inventory.HasKey(KeyColorType.Green);
                bool hasYellow = inventory.HasKey(KeyColorType.Yellow);

                if (hasGreen || hasYellow) 
                {
                    if (!door1Panel.activeSelf) door1Panel.SetActive(true);
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

            if (!isDoor2Finished)
            {
                bool hasBlue = inventory.HasKey(KeyColorType.Blue);
                bool hasPink = inventory.HasKey(KeyColorType.Pink);

                if (hasBlue || hasPink)
                {
                    if (!door2Panel.activeSelf) door2Panel.SetActive(true);
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

        private void UpdateSingleTask(TextMeshProUGUI textComp, Image icon, GameObject lineObj, bool isCollected, string originalText)
        {
            if (textComp != null)
            {
                if (isCollected)
                {
                    textComp.color = activeColor;
                    textComp.text = originalText;
                    
                    if(lineObj != null) lineObj.SetActive(true);
                }
                else
                {
                    textComp.color = inactiveColor;
                    textComp.text = originalText;
                    
                    if(lineObj != null) lineObj.SetActive(false);
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
                panel.SetActive(false);
            }
        }
    }
}