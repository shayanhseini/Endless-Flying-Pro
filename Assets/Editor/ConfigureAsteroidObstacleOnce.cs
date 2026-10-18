#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class ConfigureAsteroidObstacleOnce
{
    private const string SessionKey = "EndlessFlying.ConfigureAsteroidObstacle.v3";
    private const string ObstaclePath = "Assets/Prefabs/Game/Obstacle.prefab";
    private const string MaterialPath = "Assets/Materials/Asteroid.mat";

    private static readonly string[] RockPaths =
    {
        "Assets/Enviroment/Models/Rocks/Rock-01/Rock_01.prefab",
        "Assets/Enviroment/Models/Rocks/Rock_02/Rock_02.prefab"
    };

    static ConfigureAsteroidObstacleOnce()
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

        GameObject root = PrefabUtility.LoadPrefabContents(ObstaclePath);
        if (root == null)
            return;

        SessionState.SetBool(SessionKey, true);

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
            Object.DestroyImmediate(rootRenderer);

        MeshFilter rootFilter = root.GetComponent<MeshFilter>();
        if (rootFilter != null)
            Object.DestroyImmediate(rootFilter);

        BoxCollider boxCollider = root.GetComponent<BoxCollider>();
        if (boxCollider != null)
            Object.DestroyImmediate(boxCollider);

        SphereCollider sphereCollider = root.GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = root.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 0.7f;

        GameObject[] variants = new GameObject[RockPaths.Length];
        Renderer firstRenderer = null;
        for (int i = 0; i < RockPaths.Length; i++)
        {
            GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPaths[i]);
            if (rockPrefab == null)
                continue;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, root.transform);
            visual.name = $"AsteroidVariant_{i + 1}";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            visual.SetActive(i == 0);
            variants[i] = visual;

            if (firstRenderer == null)
                firstRenderer = visual.GetComponentInChildren<Renderer>(true);
        }

        Material asteroidMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (asteroidMaterial == null && firstRenderer != null)
        {
            asteroidMaterial = new Material(firstRenderer.sharedMaterial);
            asteroidMaterial.name = "Asteroid";
            AssetDatabase.CreateAsset(asteroidMaterial, MaterialPath);
        }

        if (asteroidMaterial != null)
        {
            asteroidMaterial.color = new Color(0.22f, 0.25f, 0.30f, 1f);
            if (asteroidMaterial.HasProperty("_EmissionColor"))
            {
                asteroidMaterial.EnableKeyword("_EMISSION");
                asteroidMaterial.SetColor("_EmissionColor", new Color(0.015f, 0.035f, 0.06f));
            }
            EditorUtility.SetDirty(asteroidMaterial);

            foreach (GameObject variant in variants)
            {
                if (variant == null)
                    continue;

                foreach (Renderer renderer in variant.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = asteroidMaterial;
            }
        }

        Obstacle obstacle = root.GetComponent<Obstacle>();
        SerializedObject obstacleObject = new(obstacle);
        obstacleObject.FindProperty("box").objectReferenceValue = variants[0];
        SerializedProperty variantsProperty = obstacleObject.FindProperty("asteroidVariants");
        variantsProperty.arraySize = variants.Length;
        for (int i = 0; i < variants.Length; i++)
            variantsProperty.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
        obstacleObject.FindProperty("minRotationSpeed").floatValue = 20f;
        obstacleObject.FindProperty("maxRotationSpeed").floatValue = 55f;
        obstacleObject.FindProperty("minScale").floatValue = 3f;
        obstacleObject.FindProperty("maxScale").floatValue = 4f;
        obstacleObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ObstaclePath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Asteroid setup complete v3] Obstacle prefab now uses two random rock variants with scale 3-4.");
    }
}
#endif
