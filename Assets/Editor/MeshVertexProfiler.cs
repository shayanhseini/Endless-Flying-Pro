using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MeshVertexProfiler
{
    [MenuItem("Tools/Profile Mesh Vertices")]
    public static void ProfileMeshes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        var list = new List<string>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            int totalVerts = 0;
            int totalTris = 0;

            foreach (var obj in objects)
            {
                if (obj is Mesh mesh)
                {
                    totalVerts += mesh.vertexCount;
                    totalTris += mesh.triangles.Length / 3;
                }
            }

            if (totalVerts > 0)
            {
                list.Add($"{totalVerts,7} verts | {totalTris,7} tris | {Path.GetFileName(path)} | {path}");
            }
        }

        list.Sort((a, b) =>
        {
            int va = int.Parse(a.Substring(0, 7).Trim());
            int vb = int.Parse(b.Substring(0, 7).Trim());
            return vb.CompareTo(va);
        });

        string result = string.Join("\n", list.Take(40));
        File.WriteAllText("Temp/mesh_vertex_profile.txt", result);
        Debug.Log("[MeshVertexProfiler] Top 40 heaviest meshes saved to Temp/mesh_vertex_profile.txt");
    }

    [MenuItem("Tools/Optimize Heavy Meshes")]
    public static void OptimizeHeavyMeshes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int count = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.Contains("/Plane/") || path.Contains("/Player/"))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            var fi = new FileInfo(path);
            // Target all meshes larger than 500 KB or known heavy models
            bool isHeavy = fi.Exists && (fi.Length > 500 * 1024 || path.Contains("Box") || path.Contains("Ground") || path.Contains("awacs") || path.Contains("Update-05"));
            if (isHeavy)
            {
                importer.meshCompression = ModelImporterMeshCompression.High;
                importer.optimizeMeshVertices = true;
                importer.optimizeMeshPolygons = true;
                importer.weldVertices = true;
                importer.isReadable = false;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/opt_heavy_meshes_done.txt", $"OPTIMIZED:{count}");
        Debug.Log($"[MeshVertexProfiler] Successfully applied High Mesh Compression and Vertex Cache Optimization to {count} heavy models!");
    }
}
