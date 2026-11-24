using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Mirage.Impostors;
using Mirage.Impostors.Core;
using Mirage.Impostors.Elements;

public static class MirageFinalChunkBaker
{
    private static readonly string[] PrefabPaths = new string[]
    {
        "Assets/Update-05/prefabs/awacs.prefab",
        "Assets/Update-05/prefabs/shelter.prefab",
        "Assets/Update-05/prefabs/building01.prefab",
        "Assets/Update-05/prefabs/building02.prefab",
        "Assets/Update-05/prefabs/main-building.prefab",
        "Assets/Update-05/prefabs/Power_Tower.prefab",
        "Assets/Update-05/prefabs/dchumvee.prefab",
        "Assets/Update-05/prefabs/helicopter.prefab",
        "Assets/Update-05/prefabs/borge.prefab",
        "Assets/Update-05/prefabs/radar.prefab"
    };

    [MenuItem("Mirage/Bake Final Chunk Props")]
    public static void BakeAll()
    {
        Debug.Log("[MirageBaker] Starting Mirage Impostor generation for final chunk heavy props...");

        string outputFolder = "Assets/Mirage/Generated/FinalChunk";
        if (!AssetDatabase.IsValidFolder("Assets/Mirage/Generated"))
        {
            AssetDatabase.CreateFolder("Assets/Mirage", "Generated");
        }
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Mirage/Generated", "FinalChunk");
        }

        // Create an optimized preset specifically for WebGL real-time performance
        ImpostorPreset preset = ScriptableObject.CreateInstance<ImpostorPreset>();
        preset.resIndex = 3; // 1024x1024
        preset.latitudeSamples = 2; // -10 to +10 degrees elevation
        preset.latitudeOffset = 2;
        preset.latitudeAngularStep = 5f;
        preset.longitudeSamples = 16; // 16 horizontal angles (every 22.5 deg)
        preset.type = SphereType.UV;
        preset.lightingMethod = LightingMethod.SurfaceEstimation;

        ImpostorLODGroupPreset lodPreset = ScriptableObject.CreateInstance<ImpostorLODGroupPreset>();
        lodPreset.setupLOD = true;
        lodPreset.lodPerformance = 0.20f; // Switch to impostor when screen height < 20%
        lodPreset.lodSizeCulling = 0.01f; // Cull when screen height < 1%

        int successCount = 0;
        foreach (string path in PrefabPaths)
        {
            try
            {
                if (BakePrefab(path, preset, lodPreset, outputFolder))
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MirageBaker] Error baking {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MirageBaker] Finished! Successfully baked impostors for {successCount}/{PrefabPaths.Length} prefabs.");
    }

    private static bool BakePrefab(string prefabPath, ImpostorPreset preset, ImpostorLODGroupPreset lodPreset, string outputFolder)
    {
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogWarning($"[MirageBaker] Prefab not found at path: {prefabPath}");
            return false;
        }

        string prefabName = sourcePrefab.name;
        Debug.Log($"[MirageBaker] Processing {prefabName}...");

        // Instantiate in scene temporarily to access mesh filters and world matrices
        GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(sourcePrefab);

        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // Clean any previous impostor instances or LOD groups if already baked
        LODGroup existingLod = instance.GetComponent<LODGroup>();
        if (existingLod != null)
        {
            UnityEngine.Object.DestroyImmediate(existingLod);
        }
        for (int i = instance.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = instance.transform.GetChild(i);
            if (child.name.ToLower().Contains("impostor"))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        try
        {
            List<MeshFilter> meshFilters = MirageEditorUtilities.GetAllDescendantMeshFilters(instance);
            if (meshFilters == null || meshFilters.Count == 0)
            {
                Debug.LogWarning($"[MirageBaker] No MeshFilters found in {prefabName}");
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            ImpostorGeometrySolver geometrySolver = new ImpostorGeometrySolver(ref preset);
            float orthographicSize = geometrySolver.GetOrthgraphicCameraSize(meshFilters);
            Bounds commonBounds = geometrySolver.GetBounds(meshFilters);
            Vector3 boundsCenter = commonBounds.center;
            List<Vector3> povs = geometrySolver.GetPOVs(orthographicSize + 0.01f);

            List<Mesh> meshes = new List<Mesh>();
            List<Material[]> materials = new List<Material[]>();
            List<Matrix4x4> transforms = new List<Matrix4x4>();

            for (int i = 0; i < meshFilters.Count; ++i)
            {
                if (meshFilters[i] == null) continue;
                MeshRenderer mr = meshFilters[i].GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                meshes.Add(meshFilters[i].sharedMesh);
                materials.Add(mr.sharedMaterials);
                Matrix4x4 translation = Matrix4x4.Translate(-boundsCenter);
                transforms.Add(translation * mr.localToWorldMatrix);
            }

            if (meshes.Count == 0)
            {
                Debug.LogWarning($"[MirageBaker] No active mesh renderers found in {prefabName}");
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            // Texture size: 1024 for heavy hero models like AWACS, 512 for secondary models
            int atlasSize = (prefabName.Contains("awacs") || prefabName.Contains("building") || prefabName.Contains("shelter")) ? 1024 : 512;

            IBakingEngine bakingEngine = new EditorBakingEngine(
                povs,
                meshes,
                materials,
                transforms,
                atlasSize,
                orthographicSize,
                preset.lightingMethod
            );

            Texture2D colorMap = bakingEngine.ComputeColorMaps();
            bakingEngine.ApplyPostProcessing(ref colorMap, colorMap);

            Texture2D normalMap = null;
            Texture2D maskMap = null;
            if (preset.lightingMethod != LightingMethod.UseSunSource)
            {
                normalMap = bakingEngine.ComputeNormalMaps();
                bakingEngine.ApplyPostProcessing(ref normalMap, colorMap);
                maskMap = bakingEngine.ComputeMaskMaps();
            }
            bakingEngine.Cleanup();

            string impostorPrefabPath = $"{outputFolder}/{prefabName}_impostor.prefab";

            ImpostorPacker packer = new ImpostorPacker();
            packer.PackImpostor(
                instance,
                meshFilters,
                colorMap,
                normalMap,
                maskMap,
                orthographicSize,
                geometrySolver.Subdivisions,
                boundsCenter,
                preset,
                lodPreset,
                impostorPrefabPath
            );

            // Save changes back to original prefab asset
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log($"[MirageBaker] Successfully baked and applied impostor for {prefabName} -> {impostorPrefabPath}");
            return true;
        }
        finally
        {
            if (instance != null)
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
