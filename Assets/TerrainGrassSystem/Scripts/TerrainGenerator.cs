using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static System.Runtime.InteropServices.Marshal;
using UnityEditor.PackageManager.UI;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public int PlaneDimension = 10;
    public int ChunkDivisions = 1;
    public int ChunkResolution = 1;
    public Material Material = default;
    public Texture2D Heightmap = default;
    public float HeightmapIntensity = 1.0f;
    public ComputeShader ChunkCompute = default;
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private int chunkAmount, chunkVertCount, chunkVertDimension;

    public struct VertData
    {
        public Vector3 Position;
        public float Height;
    }

    public struct TriData
    {
        public int3 Tri1;
        public int3 Tri2;
    }

    public struct TerrainChunk
    {
        public ComputeBuffer ArgsBuffer;
        public ComputeBuffer VertexBuffer;
        public ComputeBuffer IndexBuffer;
    }
    
    TerrainChunk[] chunks;
    uint[] args;
    
    void OnEnable()
    {
        chunkAmount = ChunkDivisions * ChunkDivisions;
        chunkVertDimension = ChunkResolution + 1;
        chunkVertCount = chunkVertDimension * chunkVertDimension;
        
        ChunkCompute.SetInt("_Dimension", PlaneDimension);
        ChunkCompute.SetInt("_ChunkResolution", ChunkResolution);
        ChunkCompute.SetInt("_ChunkDivisions", ChunkDivisions);
        ChunkCompute.SetTexture(0, "_Heightmap", Heightmap);
        
        args = new uint[5] { 0, 0, 0, 0, 0 };
        
        InitializeChunksAll();
    }

    void OnDisable()
    {
        FreeChunksAll();
    }

    private void InitializeChunksAll()
    {
        chunks = new TerrainChunk[ChunkResolution * ChunkResolution];

        for (int x = 0; x < ChunkResolution; x++)
        {
            for (int y = 0; y < ChunkResolution; y++)
            {
                chunks[x + y * ChunkResolution] = InitiateChunk(x, y);
            }
        }
    }

    private void FreeChunksAll()
    {
        for (int i = 0; i < ChunkResolution * ChunkResolution; i++)
        {
            FreeChunk(chunks[i]);
        }
    }
    
    private TerrainChunk InitiateChunk(int xOffset, int yOffset)
    {
        var chunk = new TerrainChunk();

        chunk.VertexBuffer = new ComputeBuffer(chunkVertCount, SizeOf(typeof(VertData)));
        chunk.IndexBuffer = new ComputeBuffer((ChunkResolution * ChunkResolution) * 6, sizeof(int));
        
        ChunkCompute.SetBuffer(0, "_VertBuffer", chunk.VertexBuffer);
        ChunkCompute.SetBuffer(0, "_IndexBuffer", chunk.IndexBuffer);
        ChunkCompute.SetInt("_XOffset", xOffset);
        ChunkCompute.SetInt("_YOffset", yOffset);
        ChunkCompute.Dispatch(0, Mathf.CeilToInt(chunkVertDimension / 8f), Mathf.CeilToInt(chunkVertDimension / 8f), 1);

        return chunk;
    }

    private void FreeChunk(TerrainChunk chunk)
    {
        chunk.VertexBuffer.Release();
        chunk.VertexBuffer = null;
        chunk.IndexBuffer.Release();
        chunk.IndexBuffer = null;
    }

//     private void GenerateMesh()
//     {
//         mesh = null;
//         
//         var vertices = new List<Vector3>();
//         for (var y = 0; y < ChunkResolution + 1; y++)
//         {
//             for (var x = 0; x < ChunkResolution + 1; x++)
//             {
//                 var xNorm = (double)x / ChunkResolution - .5f;
//                 var yNorm = (double)y / ChunkResolution - .5f;
//                 var xf = (float)xNorm * PlaneDimension;
//                 var yf = (float)yNorm * PlaneDimension;
//                 var u = (float) x / ChunkResolution;
//                 var v = (float) y / ChunkResolution;
//                 var height = Heightmap.GetPixelBilinear(u, v).r * HeightmapIntensity;
//                 vertices.Add(new Vector3(xf, height, yf));
//             }
//         }
//         
//         var triangles = new List<int>();
//         for (var col = 0; col < ChunkResolution; col++)
//         {
//             for (var row = 0; row < ChunkResolution; row++)
//             {
//                 int i = col * (ChunkResolution + 1) + row;
//
//                 triangles.Add(i);
//                 triangles.Add(i + ChunkResolution + 1);
//                 triangles.Add(i + ChunkResolution + 2);
//                 
//                 triangles.Add(i);
//                 triangles.Add(i + ChunkResolution + 2);
//                 triangles.Add(i + 1);
//             }
//         }
//         
//         mesh = new Mesh();
//         mesh.name = "Generated Terrain";
//         mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
//         mesh.vertices = vertices.ToArray();
//         mesh.triangles = triangles.ToArray();
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//         meshFilter.sharedMesh = mesh;
//     }
}
