using UnityEngine;

[CreateAssetMenu(menuName = "HeightGrass/GrassSettings")]
public class GrassSettings : ScriptableObject
{
    public Vector2 HeightRange;
    public Vector2 WidthRange;
    public Material GrassMat;
}

public class HeightGrass : MonoBehaviour
{
    [Header("Grass Chunk Settings")]
    [SerializeField] private int chunkDivisions = 1;
    [SerializeField] private int chunkDensity = 1;
    [SerializeField] private GrassSettings grassSettings;

    [Header("Dependencies")]
    [SerializeField] private ComputeShader heightGrassCompute;
    [SerializeField] private TerrainGenerator terrainGenerator;
    [SerializeField] private CentralizedHeight heightSystem;

    private struct InstanceData
    {
        public Vector3 position;
        public Vector2 facing;

        public float height;
        public float width;
    }

    private struct GrassChunkData
    {
        public GraphicsBuffer argsBuffer;
        public GraphicsBuffer instanceDataBuffer;
        public GraphicsBuffer indexBuffer;
    }

    private void InitGrassChunk()
    {
        //var chunk
    }
}
