using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class SetupDifficultyUI
{
    [MenuItem("Tools/Setup Difficulty UI")]
    public static void Setup()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find Start Panel
        GameObject startPanel = GameObject.Find("Canvas/Start Panel");
        if (startPanel == null)
        {
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var sp = FindRecursive(go.transform, "Start Panel");
                if (sp != null)
                {
                    startPanel = sp.gameObject;
                    break;
                }
            }
        }

        if (startPanel == null)
        {
            Debug.LogError("Could not find Start Panel in scene!");
            return;
        }

        // Deactivate TopVission (bottom-left logo)
        Transform topVission = FindRecursive(startPanel.transform, "TopVission");
        if (topVission != null)
        {
            topVission.gameObject.SetActive(false);
            Debug.Log("Deactivated TopVission logo.");
        }

        // Remove existing Difficulty Panel if present
        Transform existingPanel = startPanel.transform.Find("Difficulty Panel");
        if (existingPanel != null)
        {
            GameObject.DestroyImmediate(existingPanel.gameObject);
        }

        // Create Difficulty Panel
        GameObject diffPanelObj = new GameObject("Difficulty Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        diffPanelObj.transform.SetParent(startPanel.transform, false);
        diffPanelObj.transform.SetAsLastSibling(); // Ensure it receives clicks over background start button
        diffPanelObj.layer = 5; // UI layer

        RectTransform panelRT = diffPanelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 65f);
        panelRT.sizeDelta = new Vector2(460f, 115f);

        Image panelImg = diffPanelObj.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);
        panelImg.raycastTarget = true;

        // Load Orbitron font
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Orbitron/Orbitron-Regular SDF.asset");

        // Create Title / Label
        GameObject labelObj = new GameObject("Difficulty Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(diffPanelObj.transform, false);

        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.52f);
        labelRT.anchorMax = new Vector2(1f, 0.98f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelObj.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            labelTMP.font = font;
            labelTMP.fontSharedMaterial = font.material;
        }
        labelTMP.fontSize = 22f;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.text = "DIFFICULTY: NORMAL";
        labelTMP.color = new Color(1f, 0.85f, 0.2f, 1f);
        labelTMP.raycastTarget = false;

        // Create Slider using DefaultControls
        DefaultControls.Resources res = new DefaultControls.Resources();
        GameObject sliderObj = DefaultControls.CreateSlider(res);
        sliderObj.name = "Difficulty Slider";
        sliderObj.transform.SetParent(diffPanelObj.transform, false);

        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.08f, 0.28f);
        sliderRT.anchorMax = new Vector2(0.92f, 0.52f);
        sliderRT.offsetMin = Vector2.zero;
        sliderRT.offsetMax = Vector2.zero;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 2;
        slider.wholeNumbers = true;
        slider.value = 1;

        // Style the slider background
        Image bgImg = sliderObj.transform.Find("Background")?.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.color = new Color(0.12f, 0.18f, 0.25f, 1f);
        }

        // Style the fill
        Image fillImg = sliderObj.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fillImg != null)
        {
            fillImg.color = new Color(0.2f, 0.8f, 1f, 1f);
        }

        // Style the handle
        RectTransform handleRT = sliderObj.transform.Find("Handle Slide Area/Handle")?.GetComponent<RectTransform>();
        if (handleRT != null)
        {
            handleRT.sizeDelta = new Vector2(26f, 26f);
            Image handleImg = handleRT.GetComponent<Image>();
            if (handleImg != null)
            {
                handleImg.color = new Color(1f, 1f, 1f, 1f);
            }
        }

        // Add 3 step labels: EASY, NORMAL, HARD
        CreateStepLabel(diffPanelObj.transform, font, "EASY", new Vector2(0.08f, 0.05f), new Vector2(0.30f, 0.26f), TextAlignmentOptions.Left, new Color(0.3f, 0.9f, 0.5f, 0.8f));
        CreateStepLabel(diffPanelObj.transform, font, "NORMAL", new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.26f), TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 0.8f));
        CreateStepLabel(diffPanelObj.transform, font, "HARD", new Vector2(0.70f, 0.05f), new Vector2(0.92f, 0.26f), TextAlignmentOptions.Right, new Color(1f, 0.35f, 0.35f, 0.8f));

        // Add DifficultyUIController component
        DifficultyUIController controller = diffPanelObj.AddComponent<DifficultyUIController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("difficultySlider").objectReferenceValue = slider;
        so.FindProperty("difficultyLabel").objectReferenceValue = labelTMP;
        so.ApplyModifiedProperties();

        SetLayerRecursively(diffPanelObj, 5);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully created Difficulty UI in GameScene!");
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void CreateStepLabel(Transform parent, TMP_FontAsset font, string text, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject("Label_" + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }
        tmp.fontSize = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.text = text;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var res = FindRecursive(child, name);
            if (res != null) return res;
        }
        return null;
    }
}
