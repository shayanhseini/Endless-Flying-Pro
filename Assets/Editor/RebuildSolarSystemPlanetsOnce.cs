#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class RebuildSolarSystemPlanetsOnce
{
    private const string SessionKey = "EndlessFlying.RebuildSolarSystemPlanets.v4";
    private const string PrefabFolder = "Assets/Planets/Prefabs";
    private const float MercuryScale = 100f;
    private const float MercuryDiameterRatio = 0.383f;

    private sealed class PlanetSource
    {
        public readonly string Name;
        public readonly float DiameterRatio;
        public readonly string ModelPath;

        public PlanetSource(string name, float diameterRatio, string modelPath = null)
        {
            Name = name;
            DiameterRatio = diameterRatio;
            ModelPath = modelPath;
        }

        public float RootScale => MercuryScale * DiameterRatio / MercuryDiameterRatio;
    }

    private static readonly PlanetSource[] Sources =
    {
        new("Mercury", 0.383f, "Assets/SolarSystem/Mercury/Mercury.fbx"),
        new("Venus", 0.949f, "Assets/SolarSystem/Venus/Venus.fbx"),
        new("Earth", 1f, "Assets/SolarSystem/Earth/Earth.fbx"),
        new("Mars", 0.532f, "Assets/SolarSystem/Mars/Mars.fbx"),
        new("Jupiter", 11.21f, "Assets/SolarSystem/Jupiter/Jupiter.fbx"),
        new("Saturn", 9.45f),
        new("Uranus", 4.01f),
        new("Neptune", 3.88f)
    };

    static RebuildSolarSystemPlanetsOnce()
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

        SessionState.SetBool(SessionKey, true);

        foreach (PlanetSource source in Sources)
            UpdatePlanetPrefab(source);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BindPlanetPrefabsToOpenScenes();
        Debug.Log("[Solar System setup complete v4] Prefab models assigned without changing materials; root scales use Mercury = 100.");
    }

    private static void UpdatePlanetPrefab(PlanetSource source)
    {
        string prefabPath = $"{PrefabFolder}/{source.Name}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
            return;

        if (!string.IsNullOrEmpty(source.ModelPath))
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(source.ModelPath);
            if (modelAsset != null)
            {
                ClearChildren(root.transform);
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                NormalizeModelToUnitDiameter(model, root.transform);
            }
        }
        else
        {
            NormalizeExistingFallback(root.transform);
        }

        float scale = source.RootScale;
        root.name = source.Name;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * scale;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    private static void NormalizeExistingFallback(Transform root)
    {
        Transform body = root.Find("PlanetBody");
        if (body != null)
        {
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one;
        }

        Transform rings = root.Find("Rings");
        if (rings != null)
        {
            rings.localPosition = Vector3.zero;
            rings.localScale = new Vector3(2.25f, 2.25f, 1f);
        }
    }

    private static void NormalizeModelToUnitDiameter(GameObject model, Transform root)
    {
        if (!TryGetBounds(model, out Bounds bounds))
            return;

        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largestDimension <= Mathf.Epsilon)
            return;

        model.transform.localScale *= 1f / largestDimension;
        if (TryGetBounds(model, out bounds))
            model.transform.position += root.position - bounds.center;
    }

    private static bool TryGetBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static void BindPlanetPrefabsToOpenScenes()
    {
        PlanetSequenceManager[] managers = Object.FindObjectsByType<PlanetSequenceManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (PlanetSequenceManager manager in managers)
        {
            SerializedObject serializedManager = new(manager);
            SerializedProperty planets = serializedManager.FindProperty("planets");
            planets.arraySize = Sources.Length;

            for (int i = 0; i < Sources.Length; i++)
            {
                SerializedProperty planet = planets.GetArrayElementAtIndex(i);
                planet.FindPropertyRelative("displayName").stringValue = Sources[i].Name;
                planet.FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{Sources[i].Name}.prefab");
                planet.FindPropertyRelative("scale").floatValue = 1f;
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        if (managers.Length > 0)
            EditorSceneManager.SaveOpenScenes();
    }
}
#endif
