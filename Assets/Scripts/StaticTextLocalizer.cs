using UnityEngine;
using TMPro;
using Assets.Scripts.Localization;

[RequireComponent(typeof(TextMeshProUGUI))]
public class StaticTextLocalizer : LocalizedComponentBase
{
    [TextArea(2, 5)]
    public string englishText;

    [TextArea(2, 5)]
    public string ukrainianText;

    private TextMeshProUGUI textComp;

    private void Awake()
    {
        textComp = GetComponent<TextMeshProUGUI>();
    }

    protected override void ApplyLocalization(Language newLanguage)
    {
        if (textComp == null) textComp = GetComponent<TextMeshProUGUI>();
        textComp.text = (newLanguage == Language.Ukrainian && !string.IsNullOrEmpty(ukrainianText))
            ? ukrainianText
            : englishText;
    }
}