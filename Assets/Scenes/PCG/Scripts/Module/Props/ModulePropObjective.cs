using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Geometry;
using Geometry.Voxel;
using PCG.Module.Cluster;
using TPool;
using TPoolStatic;
using UnityEngine;

namespace PCG.Module.Prop
{
    using static PCGDefines<int>;
    public class ModulePathCollapse
    {
        public bool m_Collapsed { get; private set; }
        public Quad<bool> m_Result { get; private set; }
        public int m_Priority { get; private set; }
        public float m_Random { get; private set; }
        public readonly List<Quad<bool>> m_Possibilities = new List<Quad<bool>>();
        public IVoxel m_Voxel { get; private set; }
        public ModulePathCollapse Init(IVoxel _voxel)
        {
            m_Voxel = _voxel;
            m_Collapsed = false;
            m_Result = KQuad.False;

            m_Priority = _voxel.Identity.GetHashCode(); //int.MinValue + _voxel.Identity.location.x * 1000 + _voxel.Identity.location.y * 1000000;
            m_Random = UNoise.Value.Unit1f1((float)m_Priority/ int.MaxValue);
            return this;
        }

        public void Fill()
        {
            m_Possibilities.Clear();
            foreach (var fillPossibility in DModuleProp.kAllPossibilities)
            {
                if (UEnum.GetEnums<EQuadFacing>().LoopIndex().Any(_restriction => !m_Voxel.m_CubeSidesExists.IsFlagEnable((int)_restriction.value) && fillPossibility[_restriction.index]))
                    continue;
                m_Possibilities.Add(fillPossibility);
            }
        }

        public void Collapse()
        {
            // var index = m_Possibilities.MaxIndex(p=>p.ToPossibilityPriority());
            
            m_Result = m_Possibilities.SelectPossibility(m_Random);
            m_Possibilities.Clear();
            m_Collapsed = true;
        }

        public bool Propaganda(Dictionary<PCGID,ModulePathCollapse> _voxels)
        {
            if (m_Collapsed)
                return false;
            TSPoolStack<Quad<bool>>.Spawn(out var invalidPossibilities);
            for (int facing = 0; facing < 4; facing++)
            {
                if(!_voxels.TryGetValue(m_Voxel.m_CubeSides[facing],out var sideCollapse))
                    continue;
                int opposite = sideCollapse.m_Voxel.m_CubeSides.FindIndex(p=>p==m_Voxel.Identity);

                if (sideCollapse.m_Collapsed)
                {
                    bool oppositeConnection = sideCollapse.m_Result[opposite];
                    foreach (var possibility in m_Possibilities)
                    {
                        if(oppositeConnection==possibility[facing])
                            continue;
                        invalidPossibilities.TryPush(possibility);
                    }
                }
                else
                {
                    var oppositeConnection = sideCollapse.m_Possibilities.Any(p => p[opposite]);
                    var oppositeDeConnection = sideCollapse.m_Possibilities.Any(p => !p[opposite]);
                    foreach (var possibility in m_Possibilities)
                    {
                        var facingConnection = possibility[facing];
                        
                        if (!oppositeConnection && facingConnection)
                            invalidPossibilities.TryPush(possibility);
                        
                        if(!oppositeDeConnection && !facingConnection)
                            invalidPossibilities.TryPush(possibility);
                    }
                }
            }

            var propagandaValidate = invalidPossibilities.Count > 0;
            m_Possibilities.RemoveRange(invalidPossibilities);
            TSPoolStack<Quad<bool>>.Recycle(invalidPossibilities);
            return propagandaValidate;
        }
    }

    public struct ModulePropCollapseData
    {
        public int propIndex;
        public byte propByte;
        public bool Available => propIndex >= 0;
        public static readonly ModulePropCollapseData Invalid = new ModulePropCollapseData(){propIndex = -1,propByte = byte.MinValue};

        public static bool operator ==(ModulePropCollapseData _src, ModulePropCollapseData _dst) => _src.propByte == _dst.propByte && _src.propIndex == _dst.propIndex;
        public static bool operator !=(ModulePropCollapseData _src, ModulePropCollapseData _dst) => _src.propByte != _dst.propByte && _src.propIndex != _dst.propIndex;
        public bool Equals(ModulePropCollapseData other) => propIndex == other.propIndex && propByte == other.propByte;
        public override bool Equals(object obj) => obj is ModulePropCollapseData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(propIndex, propByte);

    }
    public class VoxelPropCollapse
    {
        public int m_Type { get; private set; }
        public IVoxel m_Voxel { get; private set; }
        public bool m_Collapsed { get; private set; }
        public ModulePropCollapseData m_Result { get; private set; }
        public int m_Priority { get; private set; }
        public byte m_MaskByte { get; private set; } 
        public byte m_BaseByte => m_Voxel.m_TypedCluster[m_Type];
        public byte VoxelByte=> (byte)(m_BaseByte & m_MaskByte);
        public VoxelPropCollapse Init(int _type,IVoxel _voxel)
        {
            m_Type = _type;
            m_Voxel = _voxel;
            m_Priority = m_Voxel.Identity.GetHashCode();
            m_Collapsed = false;
            m_Result = ModulePropCollapseData.Invalid;
            m_MaskByte = byte.MaxValue;
            return this;
        }
        public bool Available(EClusterType _clusterType,uint[] _propMasks)
        {
            if (m_Collapsed)
                return false;

            var index = UModulePropByte.GetOrientedPropIndex(_clusterType,VoxelByte).index;
            if (index == -1)
                return false;
            var maskIndex = index / 32;
            var readMask = 1 << (index - maskIndex*32);
            return (_propMasks[maskIndex] & readMask) == readMask;
        }

        public void Collapse(int _index)
        {
            m_Result = new ModulePropCollapseData()
            {
                propIndex = _index,
                propByte = VoxelByte,
            };
            m_Collapsed = true;
        }

        private static readonly byte[] kPropMasks = {
            new Qube<bool>(
                true,false,true,true,
                true,false,true,true).ToByte(),            
            new Qube<bool>(
                true,true,false,true,
                true,true,false,true).ToByte(),            
            new Qube<bool>(
                true,true,true,false,
                true,true,true,false).ToByte(),            
            new Qube<bool>(
                false,true,true,true,
                false,true,true,true).ToByte(),
        };  //Da f*ck
        public void AppendCollapseMask(PCGID _maskVoxel)
        {
            var facing =m_Voxel.m_CubeSides.FindIndex(p => p == _maskVoxel);
            m_MaskByte = (byte) ( m_MaskByte & kPropMasks[facing]);
        }
    }
    
    public class ModulePropElement:APoolItem<int>
    {
        public bool enabled
        {
            get => m_MeshRenderer.enabled;
            set => m_MeshRenderer.enabled = value;
        }
        public EModulePropType m_Type { get; private set; }
        private readonly MeshRenderer m_MeshRenderer;
        private readonly MeshFilter m_MeshFilter;
        
        private bool m_Show;
        private readonly Counter m_Counter = new Counter(.25f,true);
        private Vector3 m_Scale;

        public ModulePropElement(Transform _transform):base(_transform)
        {
            m_MeshFilter = _transform.gameObject.AddComponent<MeshFilter>();
            m_MeshRenderer = _transform.gameObject.AddComponent<MeshRenderer>();
        }

        public ModulePropElement Init(IVoxel _voxel, ModulePropData _propData,int _orientation, IList<Mesh> _meshLibrary, IList<Material> _materialLibrary)
        {
            enabled = true;
            m_Type = _propData.type;
            Transform.gameObject.name = _voxel.Identity.ToString();
            m_MeshFilter.sharedMesh = _meshLibrary[_propData.meshIndex];
            m_MeshRenderer.sharedMaterials = _propData.embedMaterialIndex.Select(p=>_materialLibrary[p]).ToArray();

            DModuleProp.OrientedToObjectVertex(_orientation,_propData.position,_voxel.m_Quad.m_ShapeOS,out var objectPosition,
                _propData.rotation,_voxel.m_Quad.m_EdgeNormalsCW,_voxel.m_Quad.m_EdgeDirectionsCW,out var objectRotation);
            Transform.position =  _voxel.Transform.localToWorldMatrix.MultiplyPoint(objectPosition);
            Transform.rotation = _voxel.Transform.rotation * objectRotation;
            m_Scale = KPCG.kPolyScale.mul(_propData.scale);
            Transform.localScale = Vector3.zero;
            
            m_Show = true;
            m_Counter.Replay();
            return this;
        }

        public void TryRecycle()
        {
            m_Show = false;
            m_Counter.Replay();
        }
        
        public bool TickRecycle(float _deltaTime)
        {
            if (!m_Counter.m_Playing)
                return false;
            m_Counter.Tick(_deltaTime);
            Transform.localScale = m_Scale * (m_Show?m_Counter.m_TimeElapsedScale:m_Counter.m_TimeLeftScale);
            if (!m_Counter.m_Playing&&!m_Show)
                return true;
            return false;
        }

        
        public override void OnPoolRecycle()
        {
            base.OnPoolRecycle();
            // m_MeshRenderer.sharedMaterials = null;
            // m_MeshFilter.sharedMesh = null;
        }
    }
}