using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
    public float HeightmapIntensity = 1.0f;
    
    public Material Material = default;
    public Texture Heightmap = default;
    public ComputeShader ChunkCompute = default;
    
    // private Mesh mesh;
    // private MeshFilter meshFilter;
    // private MeshRenderer meshRenderer;

    private int chunkAmount, chunkVertCount, chunkVertDimension, chunkIndexCount;

    struct VertData
    {
        public Vector3 Position;
        public Vector2 UV;
    }

    struct TerrainChunk
    {
        public GraphicsBuffer ArgsBuffer;
        public GraphicsBuffer VertexBuffer;
        public GraphicsBuffer IndexBuffer;
    }
    
    TerrainChunk[] chunks;
    GraphicsBuffer.IndirectDrawIndexedArgs args;
    Bounds terrainBounds;
    
    void OnEnable()
    {
        chunkAmount = ChunkDivisions * ChunkDivisions;
        chunkVertDimension = ChunkResolution + 1;
        chunkVertCount = chunkVertDimension * chunkVertDimension;
        chunkIndexCount = ChunkResolution * ChunkResolution * 6;
        
        ChunkCompute.SetInt("_Dimension", PlaneDimension);
        ChunkCompute.SetInt("_ChunkResolution", ChunkResolution);
        ChunkCompute.SetInt("_ChunkDivisions", ChunkDivisions);
        ChunkCompute.SetTexture(0, "_HeightMap", Heightmap);
        ChunkCompute.SetFloat("_HeightMapIntensity", HeightmapIntensity);
        
        args = new GraphicsBuffer.IndirectDrawIndexedArgs();
        args.baseVertexIndex = 0;
        args.indexCountPerInstance = (uint)chunkIndexCount;
        args.instanceCount = 1;
        args.startIndex = 0;
        
        InitializeChunksAll();

        terrainBounds = new Bounds(transform.position, new Vector3(PlaneDimension, HeightmapIntensity, PlaneDimension));
    }

    void Update()
    {
        Material.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
        
        for (int i = 0; i < chunkAmount; i++)
        {
            var chunk = chunks[i];
            
            var rp = new RenderParams(Material);
            rp.matProps = new MaterialPropertyBlock();
            rp.matProps.SetBuffer("_VertData", chunk.VertexBuffer);
            rp.worldBounds = terrainBounds;
            Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, chunk.IndexBuffer, chunk.ArgsBuffer);
        }
    }
    
    void OnDisable()
    {
        FreeChunksAll();
    }

    private void InitializeChunksAll()
    {
        chunks = new TerrainChunk[chunkAmount];

        for (int x = 0; x < ChunkDivisions; x++)
        {
            for (int y = 0; y < ChunkDivisions; y++)
            {
                chunks[x + y * ChunkDivisions] = InitiateChunk(x, y);
            }
        }
    }

    private void FreeChunksAll()
    {
        for (int i = 0; i < chunkAmount; i++)
        {
            FreeChunk(chunks[i]);
        }
    }
    
    private TerrainChunk InitiateChunk(int xOffset, int yOffset)
    {
        var chunk = new TerrainChunk();

        chunk.ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        chunk.VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkVertCount, SizeOf(typeof(VertData)));
        chunk.IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkIndexCount, sizeof(int));
        
        chunk.ArgsBuffer.SetData(new [] { args });
        
        ChunkCompute.SetBuffer(0, "_VertBuffer", chunk.VertexBuffer);
        ChunkCompute.SetBuffer(0, "_IndexBuffer", chunk.IndexBuffer);
        ChunkCompute.SetInt("_XOffset", xOffset);
        ChunkCompute.SetInt("_YOffset", yOffset);
        ChunkCompute.Dispatch(0, Mathf.CeilToInt(chunkVertDimension / 8f), Mathf.CeilToInt(chunkVertDimension / 8f), 1);

        return chunk;
    }

    private void FreeChunk(TerrainChunk chunk)
    {
        chunk.ArgsBuffer.Release();
        chunk.ArgsBuffer = null;
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
