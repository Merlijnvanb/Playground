using System;
using Sirenix.OdinInspector;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public CentralizedHeight HeightSystem;
    
    [SerializeField] private int planeDimension = 10;
    [SerializeField] private int chunkDivisions = 1;
    [SerializeField] private int chunkResolution = 1;
    
    [SerializeField] private Material material = default;
    [SerializeField] private ComputeShader chunkCompute = default;

    [SerializeField] private bool debugBounds = false;
    
    // private Mesh mesh;
    // private MeshFilter meshFilter;
    // private MeshRenderer meshRenderer;

    private int chunkAmount, chunkVertCount, chunkVertDimension, chunkIndexCount;
    private float chunkDimension;

    public event Action<TerrainGenerator> OnGeneration;
    

    private struct VertData
    {
        public Vector3 Position;
        public Vector2 UV;
    }

    public struct TerrainChunk
    {
        public GraphicsBuffer ArgsBuffer;
        public GraphicsBuffer IndexBuffer;
        public GraphicsBuffer VertexBuffer;
        public Bounds Bounds;
    }

    public TerrainChunk[] Chunks;
    
    GraphicsBuffer.IndirectDrawIndexedArgs args;
    Bounds terrainBounds;
    
    
    void OnEnable()
    {
        InitTerrain();
    }
    
    void OnDisable()
    {
        FreeChunksAll();
    }

    void Update()
    {
        material.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
        
        for (int i = 0; i < chunkAmount; i++)
        {
            var chunk = Chunks[i];
            
            var rp = new RenderParams(material);
            rp.matProps = new MaterialPropertyBlock();
            rp.matProps.SetBuffer("_VertData", chunk.VertexBuffer);
            rp.worldBounds = terrainBounds;
            Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, chunk.IndexBuffer, chunk.ArgsBuffer);
        }
    }

    [Button("Regenerate Terrain")]
    private void RegenerateTerrain()
    {
        FreeChunksAll();
        InitTerrain();
    }

    private void InitTerrain()
    {
        chunkAmount = chunkDivisions * chunkDivisions;
        chunkVertDimension = chunkResolution + 1;
        chunkVertCount = chunkVertDimension * chunkVertDimension;
        chunkIndexCount = chunkResolution * chunkResolution * 6;
        chunkDimension = (float)planeDimension / (float)chunkDivisions;
        
        chunkCompute.SetInt("_Dimension", planeDimension);
        chunkCompute.SetInt("_ChunkResolution", chunkResolution);
        chunkCompute.SetInt("_ChunkDivisions", chunkDivisions);
        chunkCompute.SetTexture(0, "_HeightMap", HeightSystem.GetMap());
        chunkCompute.SetFloat("_HeightMapIntensity", HeightSystem.GetMultiplier());
        chunkCompute.SetFloat("_ChunkDimension", chunkDimension);
        
        args = new GraphicsBuffer.IndirectDrawIndexedArgs();
        args.baseVertexIndex = 0;
        args.indexCountPerInstance = (uint)chunkIndexCount;
        args.instanceCount = 1;
        args.startIndex = 0;
        
        InitializeChunksAll();

        terrainBounds = new Bounds(transform.position, new Vector3(planeDimension, HeightSystem.GetMultiplier(), planeDimension));
    }

    private void InitializeChunksAll()
    {
        Chunks = new TerrainChunk[chunkAmount];

        for (int x = 0; x < chunkDivisions; x++)
        {
            for (int y = 0; y < chunkDivisions; y++)
            {
                Chunks[x + y * chunkDivisions] = InitiateChunk(x, y);
            }
        }
    }
    
    private TerrainChunk InitiateChunk(int xOffset, int yOffset)
    {
        var chunk = new TerrainChunk();

        chunk.ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        chunk.VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkVertCount, SizeOf(typeof(VertData)));
        chunk.IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkIndexCount, sizeof(int));
        
        chunk.ArgsBuffer.SetData(new [] { args });
        
        chunkCompute.SetBuffer(0, "_VertBuffer", chunk.VertexBuffer);
        chunkCompute.SetBuffer(0, "_IndexBuffer", chunk.IndexBuffer);
        chunkCompute.SetInt("_XOffset", xOffset);
        chunkCompute.SetInt("_YOffset", yOffset);
        chunkCompute.Dispatch(0, Mathf.CeilToInt(chunkVertDimension / 8f), Mathf.CeilToInt(chunkVertDimension / 8f), 1);

        var chunkLocalX = (xOffset - 0.5f * (chunkDivisions - 1)) * chunkDimension;
        var chunkLocalZ = (yOffset - 0.5f * (chunkDivisions - 1)) * chunkDimension;
        var localPos = new Vector3(chunkLocalX, HeightSystem.GetMultiplier() * 0.5f, chunkLocalZ);
        var worldPos = transform.TransformPoint(localPos);
        chunk.Bounds = new Bounds(worldPos, new Vector3(chunkDimension, HeightSystem.GetMultiplier(), chunkDimension));

        return chunk;
    }

    private void FreeChunksAll()
    {
        for (int i = 0; i < chunkAmount; i++)
        {
            FreeChunk(Chunks[i]);
        }
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
    
    void OnDrawGizmos()
    {
        if (!debugBounds)
            return;
            
        Gizmos.color = Color.green;

        foreach (var chunk in Chunks)
        {
            Gizmos.DrawWireCube(chunk.Bounds.center, chunk.Bounds.size);
        }
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
