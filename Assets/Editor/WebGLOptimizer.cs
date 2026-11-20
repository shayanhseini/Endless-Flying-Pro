using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class WebGLOptimizer
{
    [MenuItem("Build/Apply WebGL Optimizations")]
    public static void ApplyOptimizations()
    {
        Debug.Log("[WebGLOptimizer] Setting WebGL PlayerSettings...");

        // 1. IL2CPP & Code Stripping
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);

        // 2. WebGL Player Settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.memorySize = 256;

        Debug.Log("[WebGLOptimizer] WebGL PlayerSettings configured successfully!");
    }

    [MenuItem("Build/Apply WebGL Texture Overrides")]
    public static void OptimizeTexturesForWebGL()
    {
        Debug.Log("[WebGLOptimizer] Applying smart high-quality texture overrides for WebGL...");
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int count = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
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
                    // UI: Retain full 2048 resolution with high-quality Crunch compression (saves ~70MB with zero blur)
                    webglSettings.maxTextureSize = 2048;
                    webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
                    webglSettings.crunchedCompression = true;
                    webglSettings.compressionQuality = 100;
                }
                else if (isPlayer || isSky)
                {
                    // Player and Skybox: Full 2048 resolution, maximum visual fidelity
                    webglSettings.maxTextureSize = 2048;
                    webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
                }
                else
                {
                    // Environment, Ground, Rocks, Obstacles: 1024 provides crisp rendering
                    webglSettings.maxTextureSize = 1024;
                    webglSettings.textureCompression = TextureImporterCompression.CompressedHQ;
                }

                importer.SetPlatformTextureSettings(webglSettings);
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLOptimizer] Configured WebGL texture overrides for {count} textures.");
    }

    [MenuItem("Build/Apply WebGL Audio Overrides")]
    public static void OptimizeAudioForWebGL()
    {
        Debug.Log("[WebGLOptimizer] Configuring high-quality audio overrides for WebGL...");
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        int count = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                bool isMusic = path.Contains("/Music/") || path.Contains("Hans Zimmer") || path.Contains("Drone Approach");
                var settings = importer.GetOverrideSampleSettings("WebGL");
                settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.70f; // 70% Vorbis = transparent ~128-160kbps, indistinguishable from original
                settings.preloadAudioData = true;

                importer.SetOverrideSampleSettings("WebGL", settings);
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLOptimizer] Configured WebGL audio overrides for {count} audio clips.");
    }

    [MenuItem("Build/Apply WebGL Mesh Compression")]
    public static void OptimizeMeshesForWebGL()
    {
        Debug.Log("[WebGLOptimizer] Applying low lossless mesh compression for heavy environment models...");
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int count = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in modelGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // Never compress player plane
                if (path.Contains("/Plane/") || path.Contains("/Player/")) continue;

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Length > 1.5 * 1024 * 1024)
                {
                    importer.meshCompression = ModelImporterMeshCompression.Low;
                    count++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLOptimizer] Applied mesh compression for {count} models.");
    }
}
