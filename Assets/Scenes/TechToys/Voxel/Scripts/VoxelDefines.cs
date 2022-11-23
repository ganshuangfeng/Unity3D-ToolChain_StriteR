using Unity.Mathematics;
using UnityEngine;

namespace TheVoxel
{
    public enum EVoxelType
    {
        Air = -1,
        Dirt,
        Grass,
        Stone,
    }
    
    public static class DVoxel
    {
        public const float kVoxelSize = 2f;

        public const int kVisualizeRange = 0;
        
        public const int kChunkSize = 128;
        public const int kTerrainHeight = 80;

        public static Int2 GetChunkID(Vector3 _positionWS) => new Int2((int)(_positionWS.x/kVoxelSize*kChunkSize),(int)(_positionWS.z/kVoxelSize*kChunkSize));
        public static Vector3 GetChunkPositionWS(Int2 _chunkID) => new Vector3(_chunkID.x*kChunkSize*kVoxelSize,-kTerrainHeight*kVoxelSize,_chunkID.y*kChunkSize*kVoxelSize);
        public static Vector3 GetVoxelPositionOS(Int3 _voxelID) => new Vector3(_voxelID.x*kVoxelSize,_voxelID.y*kVoxelSize,_voxelID.z*kVoxelSize);

        public static float4 GetVoxelBaseColor(EVoxelType _type)
        {
            switch (_type)
            {
                case EVoxelType.Dirt: return new float4(0.2745098f, 0.2350964f, 0.1764706f, 1f);
                case EVoxelType.Grass: return new float4(0.2741982f, 0.5377358f, 0.2671769f, 1f);
                case EVoxelType.Stone: return new float4(0.612775f, 0.6320754f, 0.6121988f, 1f);
                
            }

            return default;
        }
    }

    public static class UVoxel
    {
    }
    
}