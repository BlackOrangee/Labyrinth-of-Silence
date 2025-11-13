using UnityEngine;
using UnityEditor;

public class RaiseLowerTerrain : EditorWindow
{
    float changeMeters = 1f; // на сколько метров поднимать/опускать

    [MenuItem("Tools/Raise/Lower Terrain")]
    public static void ShowWindow()
    {
        GetWindow<RaiseLowerTerrain>("Raise/Lower Terrain");
    }

    void OnGUI()
    {
        GUILayout.Label("Изменить высоту Terrain", EditorStyles.boldLabel);
        changeMeters = EditorGUILayout.FloatField("Метров:", changeMeters);

        if (GUILayout.Button("Поднять"))
        {
            ModifyTerrain(changeMeters);
        }

        if (GUILayout.Button("Опустить"))
        {
            ModifyTerrain(-changeMeters);
        }
    }

    void ModifyTerrain(float meters)
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выдели Terrain в Hierarchy!", "OK");
            return;
        }

        Terrain terrain = selected.GetComponent<Terrain>();
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выделенный объект не содержит Terrain!", "OK");
            return;
        }

        TerrainData data = terrain.terrainData;
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;

        float[,] heights = data.GetHeights(0, 0, w, h);
        float scale = meters / data.size.y; // переводим метры в нормализованную высоту

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                heights[y, x] = Mathf.Clamp01(heights[y, x] + scale);
            }
        }

        data.SetHeights(0, 0, heights);
        Debug.Log($"Terrain изменён на {meters} метров!");
    }
}
