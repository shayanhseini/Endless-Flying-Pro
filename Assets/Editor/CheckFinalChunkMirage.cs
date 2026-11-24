using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirage.Impostors;

public static class CheckFinalChunkMirage
{
    public static void RunDiagnostic()
    {
        string reportPath = "Temp/mirage_diagnostic_report.txt";
        List<string> lines = new List<string>();
        lines.Add("=== FINAL CHUNK & MIRAGE DIAGNOSTIC REPORT ===");
        lines.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        GameObject finalEnvPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/EXP/Final Env.prefab");
        if (finalEnvPrefab == null)
        {
            lines.Add("ERROR: Assets/Prefabs/Game/EXP/Final Env.prefab NOT FOUND!");
            File.WriteAllLines(reportPath, lines);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(finalEnvPrefab) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(finalEnvPrefab);

        try
        {
            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);
            lines.Add($"Total Transform hierarchy count: {allTransforms.Length}");

            // Group top-level props
            List<Transform> topProps = new List<Transform>();
            for (int i = 0; i < instance.transform.childCount; i++)
            {
                Transform child = instance.transform.GetChild(i);
                if (child.name.Contains("Enve_Objects"))
                {
                    for (int j = 0; j < child.childCount; j++)
                    {
                        topProps.Add(child.GetChild(j));
                    }
                }
                else
                {
                    topProps.Add(child);
                }
            }

            lines.Add($"\n--- DIRECT PROPS BREAKDOWN (Total: {topProps.Count}) ---");
            Dictionary<string, int> propCounts = new Dictionary<string, int>();
            Dictionary<string, int> activeCounts = new Dictionary<string, int>();
            Dictionary<string, int> lodCounts = new Dictionary<string, int>();

            foreach (Transform t in topProps)
            {
                string baseName = t.name;
                int paren = baseName.IndexOf('(');
                if (paren > 0) baseName = baseName.Substring(0, paren).Trim();

                if (!propCounts.ContainsKey(baseName))
                {
                    propCounts[baseName] = 0;
                    activeCounts[baseName] = 0;
                    lodCounts[baseName] = 0;
                }
                propCounts[baseName]++;
                if (t.gameObject.activeSelf) activeCounts[baseName]++;

                LODGroup lod = t.GetComponent<LODGroup>();
                if (lod != null)
                {
                    lodCounts[baseName]++;
                }
            }

            foreach (var kvp in propCounts)
            {
                string name = kvp.Key;
                lines.Add($"Prop: {name,-22} | Total: {kvp.Value,2} | Active: {activeCounts[name],2} | Inactive: {kvp.Value - activeCounts[name],2} | Has LODGroup/Mirage: {lodCounts[name],2}");
            }

            lines.Add("\n--- DETAILED INSPECTION OF ACTIVE PROPS WITH MIRAGE ---");
            HashSet<string> inspectedPrefabs = new HashSet<string>();

            foreach (Transform t in topProps)
            {
                if (!t.gameObject.activeSelf) continue;

                string baseName = t.name;
                int paren = baseName.IndexOf('(');
                if (paren > 0) baseName = baseName.Substring(0, paren).Trim();
                if (inspectedPrefabs.Contains(baseName)) continue;
                inspectedPrefabs.Add(baseName);

                LODGroup lod = t.GetComponent<LODGroup>();
                if (lod == null)
                {
                    lines.Add($"\n[NO LOD] {t.name}: Standard Mesh (No Mirage Impostor). Scale: {t.localScale}");
                    continue;
                }

                lines.Add($"\n[LOD GROUP FOUND] {t.name}:");
                lines.Add($"  Size: {lod.size}, Local Ref: {lod.localReferencePoint}");
                LOD[] lods = lod.GetLODs();
                lines.Add($"  LOD Levels: {lods.Length}");
                for (int l = 0; l < lods.Length; l++)
                {
                    lines.Add($"    LOD {l}: Screen Height Transition = {lods[l].screenRelativeTransitionHeight}, Renderers = {lods[l].renderers?.Length ?? 0}");
                    if (lods[l].renderers != null)
                    {
                        foreach (var r in lods[l].renderers)
                        {
                            if (r == null)
                            {
                                lines.Add($"      WARNING: Null renderer in LOD {l}!");
                                continue;
                            }
                            lines.Add($"      Renderer: {r.name} ({r.GetType().Name}), Enabled: {r.enabled}");
                            foreach (var m in r.sharedMaterials)
                            {
                                if (m == null)
                                {
                                    lines.Add($"        WARNING: Null material on {r.name}!");
                                }
                                else
                                {
                                    lines.Add($"        Material: {m.name}, Shader: {m.shader?.name ?? "NULL"}");
                                    if (m.shader != null && m.shader.name.Contains("Mirage"))
                                    {
                                        Texture mainTex = m.GetTexture("_MainTex");
                                        Texture normTex = m.HasProperty("_Normals") ? m.GetTexture("_Normals") : null;
                                        lines.Add($"        Mirage Textures -> Main: {(mainTex != null ? mainTex.name : "MISSING")}, Normals: {(normTex != null ? normTex.name : "None")}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lines.Add("\n--- INACTIVE PROPS DETAIL ---");
            foreach (Transform t in topProps)
            {
                if (!t.gameObject.activeSelf)
                {
                    lines.Add($"Inactive Object in Prefab: {t.name} (Position: {t.localPosition})");
                }
            }
        }
        catch (Exception ex)
        {
            lines.Add($"EXCEPTION during diagnostic: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }

        File.WriteAllLines(reportPath, lines);
        Debug.Log($"[CheckFinalChunkMirage] Report written to {reportPath}");
    }
}
