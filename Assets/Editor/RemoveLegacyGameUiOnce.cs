#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class RemoveLegacyGameUiOnce
{
    private const string SessionKey = "EndlessFlying.PlanetAssetsAndUiCleanup.v3";

    private static readonly string[] PlanetNames =
    {
        "Mercury", "Venus", "Earth", "Mars",
        "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    private static readonly Color[] PlanetColors =
    {
        new(0.45f, 0.42f, 0.38f),
        new(0.90f, 0.62f, 0.22f),
        new(0.12f, 0.38f, 0.88f),
        new(0.75f, 0.20f, 0.08f),
        new(0.82f, 0.62f, 0.38f),
        new(0.86f, 0.76f, 0.48f),
        new(0.34f, 0.82f, 0.88f),
        new(0.12f, 0.25f, 0.78f)
    };

    private static readonly HashSet<string> UiNamesToRemove = new()
    {
        "Start Panel",
        "Total Coin Panel",
        "High Score Panel",
        "Coin_panel",
        "Speed_Panel",
        "Shield_Panel",
        "Distance_Panel"
    };

    static RemoveLegacyGameUiOnce()
    {
        EditorApplication.delayCall += Run;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "GameScene")
            return;

        GameObject sample = FindSceneObject(scene, "Sample");
        GameObject chunkManagerObject = FindSceneObject(scene, "Chunk Manager");
        if (sample == null || chunkManagerObject == null)
            return;

        SessionState.SetBool(SessionKey, true);

        RemoveLegacyUi(scene);
        CreateFolder("Assets", "Planets");
        CreateFolder("Assets/Planets", "Materials");
        CreateFolder("Assets/Planets", "Prefabs");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        GameObject[] prefabs = new GameObject[PlanetNames.Length];
        for (int i = 0; i < PlanetNames.Length; i++)
        {
            string materialPath = $"Assets/Planets/Materials/{PlanetNames[i]}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = PlanetColors[i];
            EditorUtility.SetDirty(material);

            GameObject instance = Object.Instantiate(sample);
            instance.name = PlanetNames[i];
            instance.SetActive(true);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            string prefabPath = $"Assets/Planets/Prefabs/{PlanetNames[i]}.prefab";
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        sample.SetActive(false);

        PlanetSequenceManager sequence = chunkManagerObject.GetComponent<PlanetSequenceManager>();
        if (sequence == null)
            sequence = chunkManagerObject.AddComponent<PlanetSequenceManager>();

        SerializedObject sequenceObject = new(sequence);
        sequenceObject.FindProperty("referencePlanet").objectReferenceValue = sample;
        sequenceObject.FindProperty("spawnDistance").floatValue = 1500f;
        sequenceObject.FindProperty("initialScaleMultiplier").floatValue = 0.05f;
        sequenceObject.FindProperty("fullScaleDistance").floatValue = 350f;
        sequenceObject.FindProperty("despawnDistanceBehindPlayer").floatValue = 300f;

        SerializedProperty planets = sequenceObject.FindProperty("planets");
        planets.arraySize = PlanetNames.Length;
        for (int i = 0; i < PlanetNames.Length; i++)
        {
            SerializedProperty planet = planets.GetArrayElementAtIndex(i);
            planet.FindPropertyRelative("displayName").stringValue = PlanetNames[i];
            planet.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            planet.FindPropertyRelative("fallbackColor").colorValue = PlanetColors[i];
            planet.FindPropertyRelative("scale").floatValue = 1f;
        }
        sequenceObject.ApplyModifiedPropertiesWithoutUndo();

        ChunkManager chunkManager = chunkManagerObject.GetComponent<ChunkManager>();
        SerializedObject chunkObject = new(chunkManager);
        chunkObject.FindProperty("planetSequence").objectReferenceValue = sequence;
        chunkObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(sequence);
        EditorUtility.SetDirty(chunkManager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Planet setup complete] Created 8 materials, 8 prefabs and assigned the sequence.");
    }

    private static void RemoveLegacyUi(Scene scene)
    {
        List<GameObject> matches = new();
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate != null &&
                candidate.scene == scene &&
                UiNamesToRemove.Contains(candidate.name))
                matches.Add(candidate);
        }

        foreach (GameObject match in matches)
        {
            if (match != null)
                Object.DestroyImmediate(match);
        }
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate != null && candidate.scene == scene && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static void CreateFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
