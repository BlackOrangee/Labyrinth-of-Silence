using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class TabNavigationController : MonoBehaviour
    {
        [System.Serializable]
        public class TabButton
        {
            public Button button;
            public Text highlightText;
        }

        [SerializeField] private TabButton[] tabs;
        [SerializeField] private GameObject[] tabPanels;

        private int currentTabIndex = 0;

        private void Start()
        {
            if (tabs.Length == 0 || tabPanels.Length == 0)
            {
                Debug.LogError("TabNavigationController: Tabs or panels array is empty!");
                return;
            }

            if (tabs.Length != tabPanels.Length)
            {
                Debug.LogWarning("TabNavigationController: Tabs and panels arrays have different lengths!");
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i].button != null)
                {
                    tabs[i].button.onClick.AddListener(() => SelectTab(index));
                }
            }

            SelectTab(0);
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Length || index >= tabPanels.Length)
            {
                Debug.LogError($"TabNavigationController: Invalid tab index {index}");
                return;
            }

            currentTabIndex = index;

            for (int i = 0; i < tabs.Length; i++)
            {
                bool isActive = (i == index);

                if (tabs[i].highlightText != null)
                {
                    tabs[i].highlightText.gameObject.SetActive(isActive);
                }
            }

            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(i == index);
                }
            }
        }

        public int GetCurrentTabIndex()
        {
            return currentTabIndex;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i].button != null)
                {
                    tabs[i].button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
