using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ApplyAllOptimizations
{
    [MenuItem("Build/Execute All Overrides and Save")]
    public static void ExecuteAll()
    {
        Debug.Log("[ApplyAllOptimizations] Starting complete optimization pass...");

        // 1. Audio Clips
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        int audioCount = 0;
        foreach (var guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            bool isMusic = path.Contains("/Music/") || path.Contains("Hans Zimmer") || path.Contains("Drone Approach") || path.Contains("blizzard");
            var settings = importer.GetOverrideSampleSettings("WebGL");
            settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.70f; // High quality 70% Vorbis (transparent audio)
            settings.preloadAudioData = true;

            importer.SetOverrideSampleSettings("WebGL", settings);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            audioCount++;
        }
        Debug.Log($"[ApplyAllOptimizations] Successfully configured and reimported {audioCount} audio files.");

        // 2. Textures
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int texCount = 0;
        foreach (var guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool isCubemap = importer.textureShape == TextureImporterShape.TextureCube || path.Contains("ReflectionProbe");
            bool isUI = path.Contains("/UI/") || importer.textureType == TextureImporterType.Sprite;
            bool isPlayer = path.Contains("/Plane/") || path.Contains("/Player/") || path.Contains("/Missile/");
            bool isSky = path.Contains("/Skybox/") || path.Contains("puresky");

            var webglSettings = importer.GetPlatformTextureSettings("WebGL");
            webglSettings.overridden = true;

            if (isCubemap)
            {
                webglSettings.maxTextureSize = 512;
                webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (isUI)
            {
                // UI: Retain full 2048 size with High Quality Crunch
                webglSettings.maxTextureSize = 2048;
                webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
                webglSettings.crunchedCompression = true;
                webglSettings.compressionQuality = 100;
            }
            else if (isPlayer || isSky)
            {
                // Plane and Skybox: Maximum fidelity 2048
                webglSettings.maxTextureSize = 2048;
                webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else
            {
                // Environment / ground: 1024
                webglSettings.maxTextureSize = 1024;
                webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
            }

            importer.SetPlatformTextureSettings(webglSettings);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            texCount++;
        }
        Debug.Log($"[ApplyAllOptimizations] Successfully configured and reimported {texCount} textures.");

        // 3. Meshes
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int meshCount = 0;
        foreach (var guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.Contains("/Plane/") || path.Contains("/Player/")) continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            var fi = new FileInfo(path);
            if (fi.Exists && fi.Length > 1.5 * 1024 * 1024)
            {
                importer.meshCompression = ModelImporterMeshCompression.Low;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                meshCount++;
            }
        }
        Debug.Log($"[ApplyAllOptimizations] Successfully configured and reimported {meshCount} heavy meshes.");

        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/opt_done.txt", "DONE");
    }
}
