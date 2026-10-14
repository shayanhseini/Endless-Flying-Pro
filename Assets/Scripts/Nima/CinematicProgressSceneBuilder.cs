#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CinematicProgressSceneBuilder
{
    static CinematicProgressSceneBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/UI/Build Cinematic Progress Hierarchy")]
    public static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameDurationProgress[] controllers =
            Object.FindObjectsByType<GameDurationProgress>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (GameDurationProgress controller in controllers)
        {
            if (!controller.gameObject.scene.IsValid())
                continue;

            controller.BuildPersistentHierarchy();

            Transform visualRoot = FindChildInScene(
                controller.gameObject.scene,
                "Cinematic Progress"
            );

            if (visualRoot == null)
                continue;

            visualRoot.localScale = Vector3.one * 1.2f;
            EditorUtility.SetDirty(visualRoot);

            Sprite circleSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Knob.psd"
                );

            for (int i = 1; i <= 7; i++)
            {
                Transform checkpoint = visualRoot.Find($"Checkpoint {i}");

                if (checkpoint == null)
                    continue;

                UnityEngine.UI.Image image =
                    checkpoint.GetComponent<UnityEngine.UI.Image>();

                if (image != null)
                {
                    image.sprite = circleSprite;
                    EditorUtility.SetDirty(image);
                }
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += BuildIfNeeded;
    }

    private static Transform FindChildInScene(
        UnityEngine.SceneManagement.Scene scene,
        string childName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }
        }

        return null;
    }
}
#endif
