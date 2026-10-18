#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: SolarSystem > Setup Sun
// Builds materials, applies texture import settings, creates Sun.prefab (with a point light).
// Put the whole UnityExport folder anywhere under Assets/ first.
public static class SunSetup
{
    [MenuItem("SolarSystem/Setup Sun")]
    public static void Setup()
    {
        string fbxPath = FindAsset("Sun", "t:Model", "/Sun.fbx");
        if (fbxPath == null)
        {
            EditorUtility.DisplayDialog("Sun setup", "Sun.fbx not found under Assets/.", "OK");
            return;
        }
        string root = Path.GetDirectoryName(fbxPath).Replace('\\', '/');

        // 1) texture import settings
        string texPath = root + "/Textures/Sun_Emission.png";
        var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.maxTextureSize = 4096;
            ti.mipmapEnabled = true;
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        // 2) materials
        var sunShader = AssetDatabase.LoadAssetAtPath<Shader>(root + "/Shaders/SunSurface.shader");
        var glowShader = AssetDatabase.LoadAssetAtPath<Shader>(root + "/Shaders/SunGlow.shader");
        if (sunShader == null || glowShader == null)
        {
            EditorUtility.DisplayDialog("Sun setup", "Shaders not found next to Sun.fbx (Shaders/ folder).", "OK");
            return;
        }
        Directory.CreateDirectory(root + "/Materials");
        var sunMat = GetOrCreateMaterial(root + "/Materials/Sun.mat", sunShader);
        sunMat.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(sunMat);
        var glowMat = GetOrCreateMaterial(root + "/Materials/Sun_Glow.mat", glowShader);
        EditorUtility.SetDirty(glowMat);

        // 3) instance + prefab
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "Sun";
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterial = r.gameObject.name.Contains("Glow") ? glowMat : sunMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // the light sits inside the Sun
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        var lightGo = new GameObject("Sun_Light");
        lightGo.transform.SetParent(go.transform, false);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 3f;   // tune to taste / render pipeline
        light.range = 200f;
        light.shadows = LightShadows.Soft;

        string prefabPath = root + "/Sun.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = go;
        Debug.Log("Sun ready: " + prefabPath + "  (tip: add Bloom in your post-processing Volume for the glow)");
    }

    static Material GetOrCreateMaterial(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        return m;
    }

    static string FindAsset(string name, string type, string endsWith)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " " + type))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(endsWith)) return p;
        }
        return null;
    }
}
#endif
