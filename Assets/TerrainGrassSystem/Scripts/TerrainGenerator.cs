using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
    public int Size = 10;
    public int Resolution = 10;
    public Material Material = default;
    public Texture2D Heightmap = default;
    public float HeightmapIntensity = 1.0f;
    public ComputeShader ChunkCompute = default;

    public struct TerrainChunk
    {
        
    }
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    
    void OnEnable()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
        }

        if (meshRenderer.sharedMaterial != Material)
        {
            meshRenderer.sharedMaterial = Material;
        }
        
        GenerateMesh();
        meshCollider.sharedMesh = mesh;
    }

    private void GenerateMesh()
    {
        mesh = null;
        
        var vertices = new List<Vector3>();
        for (var y = 0; y < Resolution + 1; y++)
        {
            for (var x = 0; x < Resolution + 1; x++)
            {
                var xNorm = (double)x / Resolution - .5f;
                var yNorm = (double)y / Resolution - .5f;
                var xf = (float)xNorm * Size;
                var yf = (float)yNorm * Size;
                var u = (float) x / Resolution;
                var v = (float) y / Resolution;
                var height = Heightmap.GetPixelBilinear(u, v).r * HeightmapIntensity;
                vertices.Add(new Vector3(xf, height, yf));
            }
        }
        
        var triangles = new List<int>();
        for (var col = 0; col < Resolution; col++)
        {
            for (var row = 0; row < Resolution; row++)
            {
                int i = col * (Resolution + 1) + row;

                triangles.Add(i);
                triangles.Add(i + Resolution + 1);
                triangles.Add(i + Resolution + 2);
                
                triangles.Add(i);
                triangles.Add(i + Resolution + 2);
                triangles.Add(i + 1);
            }
        }
        
        mesh = new Mesh();
        mesh.name = "Generated Terrain";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }
}
