using System.IO;
using UnityEditor;
using UnityEngine;

public class PartRegistryBuilder : EditorWindow
{
    private string registerFolderPath = "Assets/PartRegister";
    private string outputAssetPath = "Assets/PartRegister/PartRegistry.asset";

    // Hilfsklasse zum Auslesen der info.json
    [System.Serializable]
    private class PartJsonWrapper
    {
        public string slot;
        public string id;
        public string partName;
        public float weightKg;
        public float fuelUnits;
        public float thrustKn;
        public float efficiencyPercent;
        public float durabilityHp;
        public int costUsd;
        public string description;
    }

    [MenuItem("SpaceAgency/Part Registry Builder")]
    public static void ShowWindow()
    {
        GetWindow<PartRegistryBuilder>("Part Registry Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Raketen-Teile Registry Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        registerFolderPath = EditorGUILayout.TextField("PartRegister Ordner:", registerFolderPath);
        outputAssetPath = EditorGUILayout.TextField("Ziel-Registry Datei:", outputAssetPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Registry aus Ordnern generieren", GUILayout.Height(40)))
        {
            BuildRegistry();
        }
    }

    private void BuildRegistry()
    {
        if (!Directory.Exists(registerFolderPath))
        {
            EditorUtility.DisplayDialog("Fehler", $"Der Ordner '{registerFolderPath}' existiert nicht!", "OK");
            return;
        }

        // Bestehende Registry laden oder neu erstellen
        PartRegistry registry = AssetDatabase.LoadAssetAtPath<PartRegistry>(outputAssetPath);
        if (registry == null)
        {
            registry = CreateInstance<PartRegistry>();
            AssetDatabase.CreateAsset(registry, outputAssetPath);
        }

        registry.allParts.Clear();

        // Alle Unterordner (z.B. P01, P02, E01...) durchgehen
        string[] subDirectories = Directory.GetDirectories(registerFolderPath);
        System.Array.Sort(subDirectories, System.StringComparer.Ordinal);
        int importedCount = 0;

        foreach (string dir in subDirectories)
        {
            string normalizedDir = dir.Replace("\\", "/");
            string folderId = Path.GetFileName(normalizedDir);
            string jsonPath = $"{normalizedDir}/info.json";

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Überspringe '{folderId}': Keine info.json gefunden.");
                continue;
            }

            // 1. JSON einlesen
            string jsonContent = File.ReadAllText(jsonPath);
            PartJsonWrapper data = JsonUtility.FromJson<PartJsonWrapper>(jsonContent);

            // 2. ScriptableObject im Unterordner erstellen oder aktualisieren
            string partAssetPath = $"{normalizedDir}/{folderId}_Data.asset";
            RocketPartData partAsset = AssetDatabase.LoadAssetAtPath<RocketPartData>(partAssetPath);
            if (partAsset == null)
            {
                partAsset = CreateInstance<RocketPartData>();
                AssetDatabase.CreateAsset(partAsset, partAssetPath);
            }

            // Werte übertragen (falls ID in JSON leer ist, Ordnernamen nutzen)
            partAsset.id = string.IsNullOrEmpty(data.id) ? folderId : data.id;
            partAsset.slot = data.slot;
            partAsset.partName = data.partName;
            partAsset.weightKg = data.weightKg;
            partAsset.fuelUnits = data.fuelUnits;
            partAsset.thrustKn = data.thrustKn;
            partAsset.efficiencyPercent = data.efficiencyPercent;
            partAsset.durabilityHp = data.durabilityHp;
            partAsset.costUsd = data.costUsd;
            partAsset.description = data.description;

            // 3. Erstes 3D-Modell im Ordner suchen & zuweisen
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { normalizedDir });
            if (modelGuids.Length > 0)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
                partAsset.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            }

            // 4. Erste Textur im Ordner suchen & zuweisen
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { normalizedDir });
            if (textureGuids.Length > 0)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(textureGuids[0]);
                partAsset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            EditorUtility.SetDirty(partAsset);
            registry.allParts.Add(partAsset);
            importedCount++;
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Erfolg!", $"{importedCount} Bauteile wurden erfolgreich in die PartRegistry eingetragen.", "Super");
    }
}
