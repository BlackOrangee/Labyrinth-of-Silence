using UnityEngine;
using TMPro;
using Assets.Scripts;

[RequireComponent(typeof(TextMeshProUGUI))]
public class StaticTextLocalizer : MonoBehaviour
{
    public string englishText;
    public string ukrainianText;

    private TextMeshProUGUI textComp;

    private void Start()
    {
        textComp = GetComponent<TextMeshProUGUI>();
        UpdateLanguage();
    }

private void UpdateLanguage()
    {
        if (Assets.Scripts.SettingsManager.Instance != null)
        {
            string currentLang = Assets.Scripts.SettingsManager.Instance.GetCurrentLanguage().ToString();
            bool isUkr = (currentLang == "Ukrainian");
            
            textComp.text = isUkr && !string.IsNullOrEmpty(ukrainianText) ? ukrainianText : englishText;
        }
    }
}