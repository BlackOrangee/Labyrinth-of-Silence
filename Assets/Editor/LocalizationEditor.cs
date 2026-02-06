using UnityEngine;
using UnityEditor;
using Assets.Scripts.Localization;
using Assets.Scripts;

/// <summary>
/// Editor utilities for the localization system
/// Adds helpful shortcuts and validation in the Unity Editor
/// </summary>
public class LocalizationEditor : EditorWindow
{
    private Language selectedLanguage = Language.English;

    [MenuItem("Tools/Localization/Open Localization Panel")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationEditor>("Localization");
    }

    [MenuItem("Tools/Localization/Switch to English")]
    public static void SwitchToEnglish()
    {
        if (Application.isPlaying && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetGameLanguage(Language.English);
            Debug.Log("[LocalizationEditor] Switched to English");
        }
        else
        {
            Debug.LogWarning("[LocalizationEditor] Enter Play Mode to change language");
        }
    }

    [MenuItem("Tools/Localization/Switch to Ukrainian")]
    public static void SwitchToUkrainian()
    {
        if (Application.isPlaying && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetGameLanguage(Language.Ukrainian);
            Debug.Log("[LocalizationEditor] Switched to Ukrainian");
        }
        else
        {
            Debug.LogWarning("[LocalizationEditor] Enter Play Mode to change language");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization Tools", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        {
            GUILayout.Label("Current Language:", EditorStyles.label);
            selectedLanguage = (Language)EditorGUILayout.EnumPopup(selectedLanguage);

            if (GUILayout.Button("Apply Language", GUILayout.Height(30)))
            {
                if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.SetGameLanguage(selectedLanguage);
                    Debug.Log($"[LocalizationEditor] Language set to: {selectedLanguage}");
                }
            }
        }
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test language switching", MessageType.Info);
        }

        GUILayout.Space(20);

        GUILayout.Label("Scene Statistics:", EditorStyles.boldLabel);

        int localizedTextCount = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None).Length;
        int localizedImageCount = FindObjectsByType<LocalizedImage>(FindObjectsSortMode.None).Length;
        int localizedDynamicCount = FindObjectsByType<LocalizedTextDynamic>(FindObjectsSortMode.None).Length;

        EditorGUILayout.LabelField("LocalizedText components:", localizedTextCount.ToString());
        EditorGUILayout.LabelField("LocalizedImage components:", localizedImageCount.ToString());
        EditorGUILayout.LabelField("LocalizedTextDynamic components:", localizedDynamicCount.ToString());
        EditorGUILayout.LabelField("Total localized components:", (localizedTextCount + localizedImageCount + localizedDynamicCount).ToString());

        GUILayout.Space(20);

        if (GUILayout.Button("Validate Localized Components", GUILayout.Height(30)))
        {
            ValidateLocalizedComponents();
        }

        GUILayout.Space(10);

        GUILayout.Label("Quick Actions:", EditorStyles.boldLabel);

        if (GUILayout.Button("Find All LocalizedText Components"))
        {
            FindAndSelectComponents<LocalizedText>();
        }

        if (GUILayout.Button("Find All LocalizedImage Components"))
        {
            FindAndSelectComponents<LocalizedImage>();
        }

        if (GUILayout.Button("Find All LocalizedTextDynamic Components"))
        {
            FindAndSelectComponents<LocalizedTextDynamic>();
        }
    }

    private void ValidateLocalizedComponents()
    {
        int issuesFound = 0;

        LocalizedText[] localizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (var lt in localizedTexts)
        {
            if (string.IsNullOrEmpty(lt.englishText))
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedText on '{lt.gameObject.name}' is missing English text!", lt.gameObject);
                issuesFound++;
            }

            if (string.IsNullOrEmpty(lt.ukrainianText))
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedText on '{lt.gameObject.name}' is missing Ukrainian text (will fallback to English)", lt.gameObject);
            }
        }

        LocalizedImage[] localizedImages = FindObjectsByType<LocalizedImage>(FindObjectsSortMode.None);
        foreach (var li in localizedImages)
        {
            if (li.englishSprite == null)
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedImage on '{li.gameObject.name}' is missing English sprite!", li.gameObject);
                issuesFound++;
            }

            if (li.ukrainianSprite == null)
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedImage on '{li.gameObject.name}' is missing Ukrainian sprite (will fallback to English)", li.gameObject);
            }
        }

        LocalizedTextDynamic[] localizedDynamics = FindObjectsByType<LocalizedTextDynamic>(FindObjectsSortMode.None);
        foreach (var ld in localizedDynamics)
        {
            if (string.IsNullOrEmpty(ld.englishFormat))
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedTextDynamic on '{ld.gameObject.name}' is missing English format!", ld.gameObject);
                issuesFound++;
            }

            if (string.IsNullOrEmpty(ld.ukrainianFormat))
            {
                Debug.LogWarning($"[LocalizationEditor] LocalizedTextDynamic on '{ld.gameObject.name}' is missing Ukrainian format (will fallback to English)", ld.gameObject);
            }
        }

        if (issuesFound == 0)
        {
            Debug.Log("[LocalizationEditor] ✓ All localized components are valid!");
        }
        else
        {
            Debug.LogWarning($"[LocalizationEditor] Found {issuesFound} issues. Check Console for details.");
        }
    }

    private void FindAndSelectComponents<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);

        if (components.Length == 0)
        {
            Debug.Log($"[LocalizationEditor] No {typeof(T).Name} components found in scene.");
            return;
        }

        GameObject[] gameObjects = new GameObject[components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            gameObjects[i] = components[i].gameObject;
        }

        Selection.objects = gameObjects;
        Debug.Log($"[LocalizationEditor] Selected {components.Length} {typeof(T).Name} components");
    }
}

/// <summary>
/// Custom inspector for LocalizedText to show helpful hints
/// </summary>
[CustomEditor(typeof(LocalizedText))]
public class LocalizedTextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LocalizedText localizedText = (LocalizedText)target;

        if (string.IsNullOrEmpty(localizedText.englishText))
        {
            EditorGUILayout.HelpBox("English text is empty! This is required as fallback.", MessageType.Warning);
        }

        if (string.IsNullOrEmpty(localizedText.ukrainianText))
        {
            EditorGUILayout.HelpBox("Ukrainian text is empty. Will use English as fallback.", MessageType.Info);
        }
    }
}

/// <summary>
/// Custom inspector for LocalizedImage to show helpful hints
/// </summary>
[CustomEditor(typeof(LocalizedImage))]
public class LocalizedImageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LocalizedImage localizedImage = (LocalizedImage)target;

        if (localizedImage.englishSprite == null)
        {
            EditorGUILayout.HelpBox("English sprite is missing! This is required as fallback.", MessageType.Warning);
        }

        if (localizedImage.ukrainianSprite == null)
        {
            EditorGUILayout.HelpBox("Ukrainian sprite is missing. Will use English sprite as fallback.", MessageType.Info);
        }
    }
}
