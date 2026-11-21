using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

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

        // Deactivate TopVission logo
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

        // Load Sprites
        Sprite chassisSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Difficulty_Chassis.png");
        Sprite badgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Difficulty_Badge.png");
        Sprite indicatorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Difficulty_Indicator.png");

        Sprite easyNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Easy_Normal.png");
        Sprite easyActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Easy_Active.png");

        Sprite normalNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Normal_Normal.png");
        Sprite normalActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Normal_Active.png");

        Sprite hardNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Hard_Normal.png");
        Sprite hardActiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Difficulty/Button_Hard_Active.png");

        if (chassisSprite == null || badgeSprite == null || indicatorSprite == null ||
            easyNormalSprite == null || normalActiveSprite == null || hardNormalSprite == null)
        {
            Debug.LogError("Failed to load one or more Difficulty sprites from Assets/UI/Difficulty!");
            return;
        }

        // 1. Root Difficulty Panel
        GameObject diffPanelObj = new GameObject("Difficulty Panel", typeof(RectTransform));
        diffPanelObj.transform.SetParent(startPanel.transform, false);
        diffPanelObj.transform.SetAsLastSibling(); // Ensure it receives clicks over background start button
        diffPanelObj.layer = 5; // UI layer

        RectTransform panelRT = diffPanelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 65f);
        panelRT.sizeDelta = new Vector2(1070f, 220f);

        // 2. Chassis (Housing with beveled borders and side light flares)
        GameObject chassisObj = new GameObject("Chassis", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        chassisObj.transform.SetParent(diffPanelObj.transform, false);
        chassisObj.layer = 5;

        RectTransform chassisRT = chassisObj.GetComponent<RectTransform>();
        chassisRT.anchorMin = new Vector2(0.5f, 0.5f);
        chassisRT.anchorMax = new Vector2(0.5f, 0.5f);
        chassisRT.pivot = new Vector2(0.5f, 0.5f);
        chassisRT.anchoredPosition = new Vector2(0f, -20f);
        chassisRT.sizeDelta = new Vector2(1070f, 131f);

        Image chassisImg = chassisObj.GetComponent<Image>();
        chassisImg.sprite = chassisSprite;
        chassisImg.raycastTarget = false;

        // 3. Top Military Badge (Star circle + wings + "درجه سختی")
        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(diffPanelObj.transform, false);
        badgeObj.layer = 5;

        RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRT.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRT.pivot = new Vector2(0.5f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(0f, 68f);
        badgeRT.sizeDelta = new Vector2(436f, 92f);

        Image badgeImg = badgeObj.GetComponent<Image>();
        badgeImg.sprite = badgeSprite;
        badgeImg.raycastTarget = false;

        // 4. Buttons Container
        GameObject buttonsObj = new GameObject("Buttons", typeof(RectTransform));
        buttonsObj.transform.SetParent(diffPanelObj.transform, false);
        buttonsObj.layer = 5;

        RectTransform buttonsRT = buttonsObj.GetComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRT.pivot = new Vector2(0.5f, 0.5f);
        buttonsRT.anchoredPosition = new Vector2(0f, -16f);
        buttonsRT.sizeDelta = new Vector2(950f, 94f);

        // 4a. Easy Button
        GameObject easyObj = new GameObject("Button_Easy", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        easyObj.transform.SetParent(buttonsObj.transform, false);
        easyObj.layer = 5;

        RectTransform easyRT = easyObj.GetComponent<RectTransform>();
        easyRT.anchorMin = new Vector2(0.5f, 0.5f);
        easyRT.anchorMax = new Vector2(0.5f, 0.5f);
        easyRT.pivot = new Vector2(0.5f, 0.5f);
        easyRT.anchoredPosition = new Vector2(-283f, 0f);
        easyRT.sizeDelta = new Vector2(263f, 94f);

        Image easyImg = easyObj.GetComponent<Image>();
        easyImg.sprite = easyNormalSprite;
        easyImg.raycastTarget = true;

        Button easyBtn = easyObj.GetComponent<Button>();
        easyBtn.transition = Selectable.Transition.None;

        // 4b. Normal Button
        GameObject normalObj = new GameObject("Button_Normal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        normalObj.transform.SetParent(buttonsObj.transform, false);
        normalObj.layer = 5;

        RectTransform normalRT = normalObj.GetComponent<RectTransform>();
        normalRT.anchorMin = new Vector2(0.5f, 0.5f);
        normalRT.anchorMax = new Vector2(0.5f, 0.5f);
        normalRT.pivot = new Vector2(0.5f, 0.5f);
        normalRT.anchoredPosition = new Vector2(0f, 0f);
        normalRT.sizeDelta = new Vector2(360f, 94f);

        Image normalImg = normalObj.GetComponent<Image>();
        normalImg.sprite = normalActiveSprite;
        normalImg.raycastTarget = true;

        Button normalBtn = normalObj.GetComponent<Button>();
        normalBtn.transition = Selectable.Transition.None;

        // 4c. Hard Button
        GameObject hardObj = new GameObject("Button_Hard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        hardObj.transform.SetParent(buttonsObj.transform, false);
        hardObj.layer = 5;

        RectTransform hardRT = hardObj.GetComponent<RectTransform>();
        hardRT.anchorMin = new Vector2(0.5f, 0.5f);
        hardRT.anchorMax = new Vector2(0.5f, 0.5f);
        hardRT.pivot = new Vector2(0.5f, 0.5f);
        hardRT.anchoredPosition = new Vector2(283f, 0f);
        hardRT.sizeDelta = new Vector2(263f, 94f);

        Image hardImg = hardObj.GetComponent<Image>();
        hardImg.sprite = hardNormalSprite;
        hardImg.raycastTarget = true;

        Button hardBtn = hardObj.GetComponent<Button>();
        hardBtn.transition = Selectable.Transition.None;

        // 5. Indicator Arrow (Glowing yellow triangle pointing upwards)
        GameObject indicatorObj = new GameObject("Indicator_Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicatorObj.transform.SetParent(diffPanelObj.transform, false);
        indicatorObj.layer = 5;

        RectTransform indicatorRT = indicatorObj.GetComponent<RectTransform>();
        indicatorRT.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRT.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRT.pivot = new Vector2(0.5f, 0.5f);
        indicatorRT.anchoredPosition = new Vector2(0f, -64f);
        indicatorRT.sizeDelta = new Vector2(90f, 45f);

        Image indicatorImg = indicatorObj.GetComponent<Image>();
        indicatorImg.sprite = indicatorSprite;
        indicatorImg.raycastTarget = false;

        // 6. Add DifficultyUIController component and hook references
        DifficultyUIController controller = diffPanelObj.AddComponent<DifficultyUIController>();
        SerializedObject so = new SerializedObject(controller);

        so.FindProperty("easyButton").objectReferenceValue = easyBtn;
        so.FindProperty("normalButton").objectReferenceValue = normalBtn;
        so.FindProperty("hardButton").objectReferenceValue = hardBtn;

        so.FindProperty("easyImage").objectReferenceValue = easyImg;
        so.FindProperty("normalImage").objectReferenceValue = normalImg;
        so.FindProperty("hardImage").objectReferenceValue = hardImg;

        so.FindProperty("easyNormalSprite").objectReferenceValue = easyNormalSprite;
        so.FindProperty("easyActiveSprite").objectReferenceValue = easyActiveSprite;

        so.FindProperty("normalNormalSprite").objectReferenceValue = normalNormalSprite;
        so.FindProperty("normalActiveSprite").objectReferenceValue = normalActiveSprite;

        so.FindProperty("hardNormalSprite").objectReferenceValue = hardNormalSprite;
        so.FindProperty("hardActiveSprite").objectReferenceValue = hardActiveSprite;

        so.FindProperty("indicatorArrow").objectReferenceValue = indicatorRT;
        so.FindProperty("easyArrowX").floatValue = -283f;
        so.FindProperty("normalArrowX").floatValue = 0f;
        so.FindProperty("hardArrowX").floatValue = 283f;
        so.FindProperty("arrowY").floatValue = -64f;
        so.FindProperty("animateIndicator").boolValue = true;
        so.FindProperty("indicatorSpeed").floatValue = 20f;

        so.ApplyModifiedProperties();

        SetLayerRecursively(diffPanelObj, 5);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully constructed Military Sci-Fi Difficulty UI in GameScene!");
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
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
