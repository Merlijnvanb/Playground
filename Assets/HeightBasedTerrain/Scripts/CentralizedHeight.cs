using UnityEngine;

public class CentralizedHeight : MonoBehaviour
{
    [SerializeField] private Texture baseMap = default;
    [SerializeField] private float baseHeightMultiplier = 1f;

    public Texture GetMap()
    {
        return baseMap;
    }

    public float GetMultiplier()
    {
        return baseHeightMultiplier;
    }
}
