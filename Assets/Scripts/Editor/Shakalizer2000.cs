using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// SHAKALIZER 2000 - TEXTURE QUALITY COMPRESSOR
/// Reduces all large textures in the project and enables streaming
/// Saves original settings and can restore them back!
///
/// USAGE:
/// Tools → Shakalizer 2000 → Open Shakalizer Window
/// </summary>
public class Shakalizer2000 : EditorWindow
{
    private static string BackupFilePath => Path.Combine(Application.dataPath, "..", "Library", "TextureBackup.json");

    [System.Serializable]
    private class TextureBackupData
    {
        public List<TextureBackupEntry> entries = new List<TextureBackupEntry>();
    }

    [System.Serializable]
    private class TextureBackupEntry
    {
        public string path;
        public int originalMaxSize;
        public bool originalStreamingMipmaps;
        public bool originalCrunchedCompression;
        public int originalCompressionQuality;
    }
    private int targetMaxSize = 1024;
    private bool enableStreamingMipmaps = true;
    private bool enableCrunchCompression = false;
    private bool dryRun = true;
    private Vector2 scrollPos;

    private List<TextureInfo> foundTextures = new List<TextureInfo>();
    private bool isScanning = false;
    private string scanFolder = "Assets";

    private class TextureInfo
    {
        public string path;
        public int currentMaxSize;
        public long estimatedMemoryMB;
        public bool streamingEnabled;
        public TextureImporter importer;
    }

    [MenuItem("Tools/Shakalizer 2000/Open Shakalizer Window")]
    private static void OpenWindow()
    {
        Shakalizer2000 window = GetWindow<Shakalizer2000>("Shakalizer 2000");
        window.minSize = new Vector2(600, 450);
        window.Show();
    }

    [MenuItem("Tools/Shakalizer 2000/Quick: Shakalize Laboratory")]
    private static void QuickOptimizeLaboratory()
    {
        OptimizeFolder("Assets/Props/Laboratory", 1024, true, false);
    }

    [MenuItem("Tools/Shakalizer 2000/Quick: Shakalize Trees")]
    private static void QuickOptimizeTrees()
    {
        OptimizeFolder("Assets/Props/Tree", 512, true, false);
    }

    [MenuItem("Tools/Shakalizer 2000/Reset to Unity Defaults (No Backup Needed)")]
    private static void ResetToUnityDefaults()
    {
        if (!EditorUtility.DisplayDialog(
            "Reset to Unity Defaults?",
            "Reset ALL textures to Unity defaults:\n\n" +
            "Max Size: 2048px\n" +
            "Streaming Mipmaps: OFF\n" +
            "Crunch Compression: OFF\n\n" +
            "Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        string folder = EditorUtility.OpenFolderPanel("Select folder to reset", "Assets", "");
        if (string.IsNullOrEmpty(folder))
            return;

        if (!folder.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "Folder must be inside Assets!", "OK");
            return;
        }

        string relativePath = "Assets" + folder.Substring(Application.dataPath.Length);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relativePath });

        int reset = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.maxTextureSize = 2048;
                importer.streamingMipmaps = false;
                importer.crunchedCompression = false;
                importer.compressionQuality = 50;
                importer.textureCompression = TextureImporterCompression.Compressed;

                var settings = importer.GetDefaultPlatformTextureSettings();
                settings.maxTextureSize = 2048;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(settings);

                importer.SaveAndReimport();
                reset++;
            }

            if (reset % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("Resetting", $"{reset}/{guids.Length}", (float)reset / guids.Length);
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[Shakalizer2000] Reset {reset} textures to Unity defaults");
        EditorUtility.DisplayDialog("Success", $"Reset {reset} textures to Unity defaults.", "OK");
    }

    [MenuItem("Tools/Shakalizer 2000/Restore From Backup")]
    private static void RestoreOriginalSettings()
    {
        if (!File.Exists(BackupFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Backup file not found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Restore Original Settings?",
            "Restore ALL textures to original settings?\n" +
            "Unity will reimport them.",
            "Yes", "Cancel"))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(BackupFilePath);
            TextureBackupData backup = JsonUtility.FromJson<TextureBackupData>(json);

            int restored = 0;
            foreach (var entry in backup.entries)
            {
                TextureImporter importer = AssetImporter.GetAtPath(entry.path) as TextureImporter;
                if (importer != null)
                {
                    importer.maxTextureSize = entry.originalMaxSize;
                    importer.streamingMipmaps = entry.originalStreamingMipmaps;
                    importer.crunchedCompression = entry.originalCrunchedCompression;
                    importer.compressionQuality = entry.originalCompressionQuality;

                    var settings = importer.GetDefaultPlatformTextureSettings();
                    settings.maxTextureSize = entry.originalMaxSize;
                    importer.SetPlatformTextureSettings(settings);

                    importer.SaveAndReimport();
                    restored++;
                }

                if (restored % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar("Restoring", $"{restored}/{backup.entries.Count}", (float)restored / backup.entries.Count);
                }
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"[Shakalizer2000] Restored {restored} textures to original settings");
            EditorUtility.DisplayDialog("Success", $"Restored {restored} textures.", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Shakalizer2000] Restore error: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to restore textures:\n{e.Message}", "OK");
        }
    }

    [MenuItem("Tools/Shakalizer 2000/Check Backup Status")]
    private static void CheckBackupStatus()
    {
        if (!File.Exists(BackupFilePath))
        {
            EditorUtility.DisplayDialog("Backup Status", "Backup file not found.", "OK");
            return;
        }

        try
        {
            string json = File.ReadAllText(BackupFilePath);
            TextureBackupData backup = JsonUtility.FromJson<TextureBackupData>(json);

            FileInfo fileInfo = new FileInfo(BackupFilePath);
            string message = $"Textures in backup: {backup.entries.Count}\n" +
                           $"Created: {fileInfo.LastWriteTime}\n" +
                           $"Size: {fileInfo.Length / 1024f:F2} KB";

            EditorUtility.DisplayDialog("Backup Status", message, "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to read backup:\n{e.Message}", "OK");
        }
    }

    [MenuItem("Tools/Shakalizer 2000/Quick: Scan All Textures > 1024px")]
    private static void QuickScanLarge()
    {
        List<string> largePaths = new List<string>();
        string[] allTextures = AssetDatabase.FindAssets("t:Texture2D");

        EditorUtility.DisplayProgressBar("Scanning", "Searching for large textures...", 0);

        for (int i = 0; i < allTextures.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allTextures[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.maxTextureSize > 1024)
            {
                largePaths.Add($"{path} ({importer.maxTextureSize}px)");
            }

            if (i % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("Scanning", $"{i}/{allTextures.Length}", (float)i / allTextures.Length);
            }
        }

        EditorUtility.ClearProgressBar();

        string message = largePaths.Count > 0
            ? $"Found {largePaths.Count} textures > 1024px:\n\n" + string.Join("\n", largePaths.Take(20))
            : "All textures already optimized (<=1024px)!";

        if (largePaths.Count > 20)
            message += $"\n\n...and {largePaths.Count - 20} more";

        EditorUtility.DisplayDialog("Scan Results", message, "OK");
    }

    void OnGUI()
    {
        GUILayout.Label("SHAKALIZER 2000 - TEXTURE QUALITY COMPRESSOR", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Shakalization Settings:", EditorStyles.boldLabel);

        targetMaxSize = EditorGUILayout.IntSlider("Target Max Size", targetMaxSize, 256, 2048);
        EditorGUILayout.HelpBox(
            targetMaxSize >= 2048 ? "2048px - no changes (current size)" :
            targetMaxSize >= 1024 ? "1024px - good quality, saves ~50%" :
            targetMaxSize >= 512 ? "512px - medium quality, saves ~75%" :
            "256px - low quality, saves ~90%",
            targetMaxSize >= 1024 ? MessageType.Info : MessageType.Warning
        );

        EditorGUILayout.Space();
        enableStreamingMipmaps = EditorGUILayout.Toggle("Enable Streaming Mipmaps", enableStreamingMipmaps);
        EditorGUILayout.HelpBox("Streaming mipmaps = load only needed resolution", MessageType.Info);

        EditorGUILayout.Space();
        enableCrunchCompression = EditorGUILayout.Toggle("Enable Crunch Compression", enableCrunchCompression);
        if (enableCrunchCompression)
            EditorGUILayout.HelpBox("Crunch compresses more but may slightly reduce quality", MessageType.Warning);

        EditorGUILayout.Space();
        dryRun = EditorGUILayout.Toggle("Dry Run (preview only)", dryRun);
        if (dryRun)
            EditorGUILayout.HelpBox("Preview mode - no changes applied", MessageType.Info);
        else
            EditorGUILayout.HelpBox("Will modify and reimport textures", MessageType.Warning);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        scanFolder = EditorGUILayout.TextField("Scan Folder:", scanFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Folder", scanFolder, "");
            if (!string.IsNullOrEmpty(selected) && selected.Contains(Application.dataPath))
            {
                scanFolder = "Assets" + selected.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("SCAN", GUILayout.Height(40)))
        {
            ScanTextures();
        }

        GUI.enabled = foundTextures.Count > 0;
        if (GUILayout.Button(dryRun ? "PREVIEW CHANGES" : "SHAKALIZE!", GUILayout.Height(40)))
        {
            ApplyOptimization();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Backup & Restore:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        bool hasBackup = File.Exists(BackupFilePath);
        if (hasBackup)
        {
            EditorGUILayout.HelpBox("Backup found", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No backup. Will be created on shakalization.", MessageType.Warning);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Check Backup"))
        {
            CheckBackupStatus();
        }

        GUI.enabled = hasBackup;
        if (GUILayout.Button("RESTORE FROM BACKUP"))
        {
            RestoreOriginalSettings();
        }
        GUI.enabled = true;

        if (GUILayout.Button("RESET TO DEFAULTS"))
        {
            ResetToUnityDefaults();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (foundTextures.Count > 0)
        {
            EditorGUILayout.LabelField($"Found textures: {foundTextures.Count}", EditorStyles.boldLabel);

            long totalMemoryMB = foundTextures.Sum(t => t.estimatedMemoryMB);
            long savedMemoryMB = foundTextures.Sum(t => EstimateMemorySavings(t));

            EditorGUILayout.HelpBox(
                $"Current memory: ~{totalMemoryMB} MB\n" +
                $"After shakalization: ~{totalMemoryMB - savedMemoryMB} MB\n" +
                $"Savings: ~{savedMemoryMB} MB ({(float)savedMemoryMB / totalMemoryMB * 100:F1}%)",
                MessageType.Info
            );

            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var tex in foundTextures)
            {
                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField(System.IO.Path.GetFileName(tex.path), GUILayout.Width(200));
                EditorGUILayout.LabelField($"{tex.currentMaxSize}px", GUILayout.Width(80));
                EditorGUILayout.LabelField($"~{tex.estimatedMemoryMB} MB", GUILayout.Width(80));

                if (!tex.streamingEnabled)
                {
                    GUILayout.Label("No Streaming", GUILayout.Width(100));
                }

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(tex.path);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        else if (!isScanning)
        {
            EditorGUILayout.HelpBox("Press SCAN to find large textures", MessageType.Info);
        }
    }

    private void ScanTextures()
    {
        foundTextures.Clear();
        isScanning = true;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { scanFolder });

        EditorUtility.DisplayProgressBar("Scanning Textures", "Анализ текстур...", 0);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.maxTextureSize > targetMaxSize)
            {
                TextureInfo info = new TextureInfo
                {
                    path = path,
                    currentMaxSize = importer.maxTextureSize,
                    streamingEnabled = importer.streamingMipmaps,
                    importer = importer,
                    estimatedMemoryMB = EstimateTextureMemory(importer.maxTextureSize)
                };

                foundTextures.Add(info);
            }

            if (i % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("Scanning Textures", $"{i}/{guids.Length}", (float)i / guids.Length);
            }
        }

        EditorUtility.ClearProgressBar();
        isScanning = false;

        Debug.Log($"[Shakalizer2000] Found {foundTextures.Count} textures larger than {targetMaxSize}px");
    }

    private void ApplyOptimization()
    {
        if (dryRun)
        {
            string preview = "WHAT WILL CHANGE:\n\n";
            foreach (var tex in foundTextures.Take(10))
            {
                preview += $"{System.IO.Path.GetFileName(tex.path)}\n";
                preview += $"  {tex.currentMaxSize}px -> {targetMaxSize}px\n";
                if (enableStreamingMipmaps && !tex.streamingEnabled)
                    preview += $"  + enable streaming\n";
                preview += $"  Saves: ~{EstimateMemorySavings(tex)} MB\n\n";
            }

            if (foundTextures.Count > 10)
                preview += $"...and {foundTextures.Count - 10} more textures\n\n";

            preview += $"TOTAL SAVINGS: ~{foundTextures.Sum(t => EstimateMemorySavings(t))} MB";

            EditorUtility.DisplayDialog("Dry Run - Preview", preview, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Confirm Shakalization",
            $"Shakalize {foundTextures.Count} textures?\n" +
            $"Memory savings: ~{foundTextures.Sum(t => EstimateMemorySavings(t))} MB\n\n" +
            $"Unity will reimport (may take time).",
            "Shakalize", "Cancel"))
        {
            return;
        }

        CreateBackup();

        int processed = 0;
        foreach (var tex in foundTextures)
        {
            EditorUtility.DisplayProgressBar("Optimizing", tex.path, (float)processed / foundTextures.Count);

            tex.importer.maxTextureSize = targetMaxSize;
            tex.importer.streamingMipmaps = enableStreamingMipmaps;

            if (enableCrunchCompression)
            {
                tex.importer.crunchedCompression = true;
                tex.importer.compressionQuality = 50;
            }

            var settings = tex.importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize = targetMaxSize;
            tex.importer.SetPlatformTextureSettings(settings);

            tex.importer.SaveAndReimport();
            processed++;
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[Shakalizer2000] Shakalized {processed} textures!");
        Debug.Log($"[Shakalizer2000] Backup saved: {BackupFilePath}");
        EditorUtility.DisplayDialog("Success", $"Shakalized {processed} textures.", "OK");

        foundTextures.Clear();
    }

    private void CreateBackup()
    {
        TextureBackupData backup = new TextureBackupData();

        if (File.Exists(BackupFilePath))
        {
            try
            {
                string existingJson = File.ReadAllText(BackupFilePath);
                backup = JsonUtility.FromJson<TextureBackupData>(existingJson);
            }
            catch
            {
                Debug.LogWarning("[Shakalizer2000] Failed to load existing backup, creating new one");
            }
        }

        foreach (var tex in foundTextures)
        {
            if (!backup.entries.Any(e => e.path == tex.path))
            {
                backup.entries.Add(new TextureBackupEntry
                {
                    path = tex.path,
                    originalMaxSize = tex.currentMaxSize,
                    originalStreamingMipmaps = tex.streamingEnabled,
                    originalCrunchedCompression = tex.importer.crunchedCompression,
                    originalCompressionQuality = tex.importer.compressionQuality
                });
            }
        }

        string json = JsonUtility.ToJson(backup, true);
        File.WriteAllText(BackupFilePath, json);

        Debug.Log($"[Shakalizer2000] Backup created: {backup.entries.Count} textures saved");
    }

    private static void OptimizeFolder(string folder, int maxSize, bool streaming, bool dryRun)
    {
        if (!System.IO.Directory.Exists(folder))
        {
            EditorUtility.DisplayDialog("Error", $"Folder not found: {folder}", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.maxTextureSize > maxSize)
            {
                if (!dryRun)
                {
                    importer.maxTextureSize = maxSize;
                    importer.streamingMipmaps = streaming;
                    importer.SaveAndReimport();
                }
                count++;
            }
        }

        string message = dryRun
            ? $"Found {count} textures for shakalization in {folder}"
            : $"Shakalized {count} textures in {folder}!";

        EditorUtility.DisplayDialog(dryRun ? "Scan Result" : "Success", message, "OK");
    }

    private long EstimateTextureMemory(int size)
    {
        return (long)(size * size * 4 * 1.33f / (1024f * 1024f));
    }

    private long EstimateMemorySavings(TextureInfo tex)
    {
        long before = EstimateTextureMemory(tex.currentMaxSize);
        long after = EstimateTextureMemory(targetMaxSize);
        return before - after;
    }
}
