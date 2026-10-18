#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: SolarSystem > Setup Mercury
// Put the whole UnityExport folder anywhere under Assets/ first.
// Works with Built-in (Standard shader) and URP (Lit). HDRP is not handled.
public static class MercurySetup
{
    [MenuItem("SolarSystem/Setup Mercury")]
    public static void Setup()
    {
        string fbxPath = FindAsset("Mercury", "t:Model", "/Mercury.fbx");
        if (fbxPath == null)
        {
            EditorUtility.DisplayDialog("Mercury setup", "Mercury.fbx not found under Assets/.", "OK");
            return;
        }
        string root = Path.GetDirectoryName(fbxPath).Replace('\\', '/');

        // textures: albedo = sRGB colour, normal = Normal map (Blender's normal map is OpenGL style, same as Unity)
        string albedoPath = root + "/Textures/Mercury_Albedo.png";
        string normalPath = root + "/Textures/Mercury_Normal.png";
        ConfigureTexture(albedoPath, false);
        ConfigureTexture(normalPath, true);
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Mercury setup", "Could not find a Lit/Standard shader (HDRP?).", "OK");
            return;
        }

        Directory.CreateDirectory(root + "/Materials");
        string matPath = root + "/Materials/Mercury.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else mat.shader = shader;

        // Blender: Metallic 0, Roughness 0.92  ->  Smoothness 0.08
        if (urp)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
        }
        else
        {
            mat.SetTexture("_MainTex", albedo);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.08f);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
        }
        mat.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat);

        // model: calculate tangents so the normal map is correct
        var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (mi != null)
        {
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.SaveAndReimport();
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "Mercury";
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            r.sharedMaterial = mat;

        string prefabPath = root + "/Mercury.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = go;
        Debug.Log("Mercury ready: " + prefabPath);
    }

    static void ConfigureTexture(string path, bool isNormal)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = !isNormal;
        ti.maxTextureSize = 2048;
        ti.mipmapEnabled = true;
        ti.wrapModeU = TextureWrapMode.Repeat;
        ti.wrapModeV = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Trilinear;
        ti.anisoLevel = 4;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
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
