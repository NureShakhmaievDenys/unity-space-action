using UnityEngine;
using UnityEditor;
using System.IO;

public class AsteroidAtlasMaker : EditorWindow
{
    public Texture2D[] textures = new Texture2D[8];
    private string saveName = "AsteroidAtlas_4x2";

    [MenuItem("Tools/Asteroid Atlas Maker")]
    public static void ShowWindow()
    {
        GetWindow<AsteroidAtlasMaker>("Atlas Maker").minSize = new Vector2(300, 400);
    }

    void OnGUI()
    {
        GUILayout.Label("Склейка атласа (4 колонки х 2 строки)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < 8; i++)
        {
            textures[i] = (Texture2D)EditorGUILayout.ObjectField($"Текстура {i + 1}", textures[i], typeof(Texture2D), false);
        }

        EditorGUILayout.Space();
        saveName = EditorGUILayout.TextField("Имя файла", saveName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Склеить и Сохранить!", GUILayout.Height(40)))
        {
            GenerateAtlas();
        }
    }

    void GenerateAtlas()
    {
        foreach (var tex in textures)
        {
            if (tex == null)
            {
                Debug.LogError("Ошибка: Заполни все 8 слотов!");
                return;
            }
        }

        // Берем размер первой текстуры (теперь они у нас по 512)
        int texWidth = textures[0].width;
        int texHeight = textures[0].height;

        // Создаем пустой холст 4x2
        Texture2D atlas = new Texture2D(texWidth * 4, texHeight * 2, TextureFormat.RGBA32, false);

        for (int i = 0; i < 8; i++)
        {
            // Делаем текстуру читаемой для скрипта
            string assetPath = AssetDatabase.GetAssetPath(textures[i]);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Математика сетки 4x2
            int col = i % 4;
            int row = i / 4;

            // Копируем пиксели
            Color[] pixels = textures[i].GetPixels();
            atlas.SetPixels(col * texWidth, row * texHeight, texWidth, texHeight, pixels);
        }

        atlas.Apply();

        // Сохраняем результат
        byte[] bytes = atlas.EncodeToPNG();
        string path = Application.dataPath + "/" + saveName + ".png";
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>ГОТОВО!</color> Ищи файл {saveName}.png в главной папке Assets.");
    }
}