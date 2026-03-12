using UnityEngine;
using UnityEditor;

public class SelectionReplacer : EditorWindow
{
    public GameObject newPrefab;

    [MenuItem("Tools/Replace Selected Objects")]
    public static void ShowWindow()
    {
        GetWindow<SelectionReplacer>("Replace Selected");
    }

    void OnGUI()
    {
        GUILayout.Label("Замена ВЫДЕЛЕННЫХ объектов", EditorStyles.boldLabel);
        newPrefab = (GameObject)EditorGUILayout.ObjectField("Новый префаб:", newPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Заменить выделенное"))
        {
            ReplaceSelected();
        }
    }

    void ReplaceSelected()
    {
        if (newPrefab == null) { Debug.LogError("Сначала выбери префаб!"); return; }
        
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0) { Debug.LogWarning("Ничего не выбрано!"); return; }

        foreach (GameObject go in selected)
        {
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
            newObj.transform.SetParent(go.transform.parent);
            newObj.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
            newObj.transform.localScale = go.transform.localScale;

            Undo.RegisterCreatedObjectUndo(newObj, "Replace Selection");
            Undo.DestroyObjectImmediate(go);
        }
    }
}