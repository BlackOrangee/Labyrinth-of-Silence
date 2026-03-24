using UnityEngine;
using UnityEditor;
using System.Reflection;

public class PrefabReplacerAdvanced : EditorWindow
{
    private GameObject oldPrefab;
    private GameObject newPrefab;

    [MenuItem("Tools/Prefab Replacer Advanced")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacerAdvanced>("Prefab Replacer Advanced");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Prefabs and keep component overrides", EditorStyles.boldLabel);

        oldPrefab = (GameObject)EditorGUILayout.ObjectField("Old Prefab", oldPrefab, typeof(GameObject), false);
        newPrefab = (GameObject)EditorGUILayout.ObjectField("New Prefab", newPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Replace Prefab Instances"))
        {
            if (oldPrefab == null || newPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Old Prefab and New Prefab!", "OK");
            }
            else
            {
                ReplacePrefabs();
            }
        }
    }

    private void ReplacePrefabs()
    {
        // [ВИПРАВЛЕНО] Використовуємо FindObjectsByType замість застарілого FindObjectsOfType
        // FindObjectsSortMode.None працює швидше, бо не витрачає час на сортування списку
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            GameObject prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (prefabRoot == oldPrefab)
            {
                // Сохраняем Transform
                Vector3 pos = go.transform.position;
                Quaternion rot = go.transform.rotation;
                Vector3 scale = go.transform.localScale;
                Transform parent = go.transform.parent;

                // Сохраняем компоненты и их поля (локальные модификации)
                Component[] components = go.GetComponents<Component>();
                
                // Создаем новый Prefab
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
                newObj.transform.position = pos;
                newObj.transform.rotation = rot;
                newObj.transform.localScale = scale;
                newObj.transform.parent = parent;

                // Копируем поля компонентов (локальные изменения)
                foreach (Component comp in components)
                {
                    if (comp == null) continue;
                    System.Type type = comp.GetType();
                    Component newComp = newObj.GetComponent(type);
                    if (newComp != null)
                    {
                        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (FieldInfo field in fields)
                        {
                            if (field.IsDefined(typeof(SerializeField), true) || field.IsPublic)
                            {
                                field.SetValue(newComp, field.GetValue(comp));
                            }
                        }
                    }
                }

                DestroyImmediate(go);
                count++;
            }
        }

        EditorUtility.DisplayDialog("Prefab Replacer", $"Replaced {count} instance(s)!", "OK");
    }
}