using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class BuildWebGL
{
    private const string TriggerPath = "Temp/trigger_webgl_build.txt";
    private const string StatusPath = "Temp/webgl_build_status.txt";
    private const string OutputFolder = "Builds/WebGL";

    static BuildWebGL()
    {
        EditorApplication.update += CheckTrigger;
    }

    [MenuItem("Build/Build WebGL (Optimized)")]
    public static void TriggerFromMenu()
    {
        File.WriteAllText(TriggerPath, "manual");
        Debug.Log("[BuildWebGL] Build triggered from Menu.");
    }

    private static void CheckTrigger()
    {
        if (EditorApplication.isCompiling)
            return;

        if (!File.Exists(TriggerPath))
            return;

        string content = "";
        try
        {
            content = File.ReadAllText(TriggerPath).Trim();
            File.Delete(TriggerPath);
        }
        catch
        {
            return;
        }

        if (content == "font_only")
        {
            Debug.Log("[BuildWebGL] Running FixFontAsset only...");
            PopulateIranSansGlyphs.FixFontAsset();
            File.WriteAllText(StatusPath, "FONT_FIXED");
            return;
        }

        if (content == "refresh")
        {
            Debug.Log("[BuildWebGL] AssetDatabase.Refresh() requested...");
            AssetDatabase.Refresh();
            File.WriteAllText(StatusPath, "REFRESHED");
            return;
        }

        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling)
        {
            Debug.Log("[BuildWebGL] Compiling scripts before build...");
            File.WriteAllText(TriggerPath, "build");
            return;
        }

        PerformBuild();
    }

    public static void PerformBuild()
    {
        try
        {
            File.WriteAllText(StatusPath, "BUILDING");
            Debug.Log("[BuildWebGL] Applying high-quality optimizations and configuring WebGL settings...");

            // Ensure DroneVision is set up in GameScene
            EnsureDroneVisionInScene();

            // Explicitly set WebGL template to DroneVision
            PlayerSettings.WebGL.template = "PROJECT:DroneVision";

            // 1. Settings, Textures, Audio, and Mesh Optimizations
            WebGLOptimizer.ApplyOptimizations();
            ApplyAllOptimizations.ExecuteAll();
            PopulateIranSansGlyphs.FixFontAsset();

            // 2. Gather active scenes
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                string fallback = "Assets/Scenes/GameScene.unity";
                if (File.Exists(fallback))
                    scenes = new[] { fallback };
                else
                    throw new Exception("No valid scenes found to build!");
            }

            string fullOutputPath = Path.GetFullPath(OutputFolder);
            if (!Directory.Exists(fullOutputPath))
            {
                Directory.CreateDirectory(fullOutputPath);
            }

            Debug.Log($"[BuildWebGL] Starting BuildPlayer with CleanBuildCache to: {fullOutputPath}");
            Debug.Log($"[BuildWebGL] Scenes ({scenes.Length}): {string.Join(", ", scenes)}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                // Ensure .nojekyll for GitHub Pages
                string nojekyllPath = Path.Combine(fullOutputPath, ".nojekyll");
                if (!File.Exists(nojekyllPath))
                {
                    File.WriteAllText(nojekyllPath, "");
                }

                // Update index.html
                string indexPath = Path.Combine(fullOutputPath, "index.html");
                if (File.Exists(indexPath))
                {
                    string html = File.ReadAllText(indexPath);

                    if (!html.Contains("unity-progress-text") && !html.Contains("loading-text"))
                    {
                        if (html.Contains("<div id=\"loading\"><div></div></div>"))
                        {
                            html = html.Replace("<div id=\"loading\"><div></div></div>",
                                "<div id=\"loading\"><div></div><div id=\"loading-text\" style=\"color: #f2c14e; font-family: sans-serif; font-size: 13px; font-weight: bold; margin-top: 10px; text-align: center;\">0%</div></div>");
                            html = html.Replace("function (p) { bar.style.width = (100 * p) + '%'; }",
                                "function (p) { bar.style.width = (100 * p) + '%'; var lt = document.getElementById('loading-text'); if (lt) lt.textContent = Math.round(100 * p) + '%'; }");
                        }
                        else if (html.Contains("<div id=\"unity-progress-bar-full\"></div>"))
                        {
                            html = html.Replace("<div id=\"unity-progress-bar-full\"></div>\n        </div>",
                                "<div id=\"unity-progress-bar-full\"></div>\n        </div>\n        <div id=\"unity-progress-text\" style=\"color: #00e5ff; font-family: sans-serif; font-size: 15px; font-weight: bold; margin-top: 12px; text-shadow: 0 0 10px rgba(0,229,255,0.7); text-align: center;\">0%</div>");
                            html = html.Replace("document.querySelector(\"#unity-progress-bar-full\").style.width = 100 * progress + \"%\";",
                                "document.querySelector(\"#unity-progress-bar-full\").style.width = 100 * progress + \"%\";\n          var pt = document.querySelector(\"#unity-progress-text\");\n          if (pt) pt.textContent = Math.round(100 * progress) + \"%\";");
                        }
                    }
                    File.WriteAllText(indexPath, html);
                }

                string info = $"SUCCESS:{summary.totalSize}:{summary.totalTime.TotalSeconds:F1}";
                File.WriteAllText(StatusPath, info);
                Debug.Log($"[BuildWebGL] BUILD SUCCEEDED in {summary.totalTime.TotalSeconds:F1}s! Output size: {summary.totalSize / (1024 * 1024)}MB");
            }
            else
            {
                string err = $"FAILED:{summary.totalErrors}";
                File.WriteAllText(StatusPath, err);
                Debug.LogError($"[BuildWebGL] BUILD FAILED with {summary.totalErrors} errors!");
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(StatusPath, $"ERROR:{ex.Message}");
            Debug.LogException(ex);
        }
    }

    private static void EnsureDroneVisionInScene()
    {
        try
        {
            string scenePath = "Assets/Scenes/GameScene.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            GameObject existing = GameObject.Find("DroneVision");
            if (existing == null)
            {
                existing = new GameObject("DroneVision");
                existing.AddComponent<DroneVision.DroneVisionInput>();
                Debug.Log("[BuildWebGL] Created DroneVision GameObject in GameScene.");
            }
            else
            {
                if (existing.GetComponent<DroneVision.DroneVisionInput>() == null)
                {
                    existing.AddComponent<DroneVision.DroneVisionInput>();
                    Debug.Log("[BuildWebGL] Added DroneVisionInput to existing DroneVision GameObject.");
                }
            }

            // The scene keeps the values it was saved with, so an older scene overrides the
            // package's measured defaults. Enforce them (DroneVision 2026-11-20):
            //   640x360 camera: bigger frames cost more and measured the same landmarks;
            //   model "auto": lite on phones / weak PCs, full elsewhere (drops to lite if slow);
            //   modelDelegate "auto": CPU on desktops, so the model never shares the GPU with the game.
            var vision = existing.GetComponent<DroneVision.DroneVisionInput>();
            vision.cameraWidth = 640;
            vision.cameraHeight = 360;
            vision.model = "auto";
            vision.modelDelegate = "auto";
            vision.maxFps = 30;
            vision.numPoses = 1;
            EditorUtility.SetDirty(vision);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[BuildWebGL] GameScene saved with DroneVision!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[BuildWebGL] Error setting up DroneVision: " + ex.Message);
        }
    }
}
