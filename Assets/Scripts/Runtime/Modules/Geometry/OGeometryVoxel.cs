using System;
using System.Collections.Generic;
using System.Linq;
using OSwizzling;
using UnityEngine;

namespace Geometry.Voxel
{
    #region Enums
    [Flags]
    public enum EQubeCorner
    {
        DB=1,
        DL=2,
        DF=8,
        DR=16,
        
        TB=32,
        TL=64,
        TF=128,
        TR=256,
    }
    
    [Flags]
    public enum ECubeFacing
    {
        LF=1,
        FR=2,
        BL=4,
        RB=8,
        T=16,
        D=32,
    }
    #endregion
    
    #region Interface
    public interface IQube<T> where T : struct
    {
        T vertDB { get; set; }
        T vertDL { get; set; }
        T vertDF { get; set; }
        T vertDR { get; set; }
        T vertTB { get; set; }
        T vertTL { get; set; }
        T vertTF { get; set; }
        T vertTR { get; set; }
        T this[int _index] { get; }
        T this[EQubeCorner _index] { get; }
    }
    public interface ICubeFace<T> where T : struct
    {
        public T fBL { get; set; }
        public T fLF { get; set; }
        public T fFR { get; set; }
        public T fRB { get; set; }
        public T fT { get; set; }
        public T fD { get; set; }
        T this[int _index] { get; }
        T this[ECubeFacing _facing] { get; }
    }
    #endregion
    
    #region Defines
    [Serializable]
    public struct GRay
    {
        public Vector3 origin;
        public Vector3 direction;
        public GRay(Vector3 _position, Vector3 _direction) { origin = _position; direction = _direction; }
        public Vector3 GetPoint(float _distance) => origin + direction * _distance;
        public static implicit operator Ray(GRay _ray)=>new Ray(_ray.origin,_ray.direction);
        public static implicit operator GRay(Ray _ray)=>new GRay(_ray.origin,_ray.direction);
        public GLine ToLine(float _length)=>new GLine(origin,direction,_length);
    }
    
    [Serializable]
    public struct GLine
    {
        public Vector3 origin;
        public Vector3 direction;
        public float length;
        public Vector3 end;
        public Vector3 GetPoint(float _distance) => origin + direction * _distance;
        public GLine(Vector3 _position, Vector3 _direction, float _length) { 
            origin = _position; 
            direction = _direction; 
            length = _length;
            end = origin + direction * length;
        }
        public static implicit operator GRay(GLine _line)=>new GRay(_line.origin,_line.direction);
    }

    [Serializable]
    public struct GTriangle:ITriangle<Vector3>,IIterate<Vector3>
    {
        public Vector3 vertex0 { get; set; }
        public Vector3 vertex1 { get; set; }
        public Vector3 vertex2 { get; set; }
        public Vector3 normal;
        public Vector3 uOffset;
        public Vector3 vOffset;
        public int Length => 3;
        public Vector3[] GetDrawLineVertices() => new Vector3[] { vertex0, vertex1, vertex2, vertex0 };
        public Vector3 this[int index] => GetElement(index);
        public Vector3 GetElement(int _index)
        {
            switch (_index)
            {
                default: Debug.LogError("Invalid Index:" + _index); return vertex0;
                case 0: return vertex0;
                case 1: return vertex1;
                case 2: return vertex2;
            }
        }

        public GTriangle((Vector3 v0,Vector3 v1,Vector3 v2) _tuple) : this(_tuple.v0,_tuple.v1,_tuple.v2)
        {
        }

        public GTriangle(Vector3 _vertex0, Vector3 _vertex1, Vector3 _vertex2)
        {
            vertex0 = _vertex0;
            vertex1 = _vertex1;
            vertex2 = _vertex2;
            uOffset = vertex1-vertex0;
            vOffset = vertex2-vertex0;
            normal= Vector3.Cross(uOffset,vOffset);
        }
        public Vector3 GetUVPoint(float u,float v)=>(1f - u - v) * vertex0 + u * uOffset + v * vOffset;
    }

    [Serializable]
    public struct GQuad:IQuad<Vector3>, IIterate<Vector3>
    {
        public Vector3 vB { get; set; }
        public Vector3 vL { get; set; }
        public Vector3 vF { get; set; }
        public Vector3 vR { get; set; }
        public Vector3 normal;

        public GQuad(Vector3 _vb, Vector3 _vl, Vector3 _vf, Vector3 _vr)
        {
            vB = _vb;
            vL = _vl;
            vF = _vf;
            vR = _vr;
            normal= Vector3.Cross(vL-vB,vF-vB);
        }
        public int Length => 4;
        public Vector3 this[int _index]=>this.GetVertex<GQuad,Vector3>(_index); 
        public Vector3 this[EQuadCorners _corner] =>this.GetVertex<GQuad,Vector3>(_corner);
        public Vector3 GetElement(int _index) => this[_index];
    }

    [Serializable]
    public struct GQube:IQube<Vector3>
    {
        public Vector3 vertDB { get; set; }
        public Vector3 vertDL { get; set; }
        public Vector3 vertDF { get; set; }
        public Vector3 vertDR { get; set; }
        public Vector3 vertTB { get; set; }
        public Vector3 vertTL { get; set; }
        public Vector3 vertTF { get; set; }
        public Vector3 vertTR { get; set; }
        
        public GQube(Vector3 _vertDB, Vector3 _vertDL, Vector3 _vertDF, Vector3 _vertDR,
            Vector3 _vertTB, Vector3 _vertTL, Vector3 _vertTF, Vector3 _vertTR)
        {
            vertDB = _vertDB;
            vertDL = _vertDL;
            vertDF = _vertDF;
            vertDR = _vertDR;
            vertTB = _vertTB;
            vertTL = _vertTL;
            vertTF = _vertTF;
            vertTR = _vertTR;
        }

        public Vector3 this[int _index] => this.GetVertex<GQube,Vector3>(_index);
        public Vector3 this[EQubeCorner _corner] => this.GetVertex<GQube,Vector3>(_corner);
    }
    
    [Serializable]
    public struct GSphere
    {
        public Vector3 center;
        public float radius;
        public GSphere(Vector3 _center,float _radius) { center = _center;radius = _radius; }
    }
    [Serializable]
    public struct GBox
    {
        public Vector3 center;
        public Vector3 extend;
        public Vector3 Size => extend * 2f;
        public Vector3 Min => center - extend;
        public Vector3 Max => center + extend;
        public GBox(Vector3 _center,Vector3 _extend) { center = _center;extend = _extend; }
    }


    [Serializable]
    public struct GPlane
    {
        public Vector3 normal;
        public float distance;
        public Vector3 Position => normal * distance;
        public GPlane(Vector3 _normal, float _distance) { normal = _normal; distance = _distance; }
        public GPlane(Vector3 _normal, Vector3 _position) : this(_normal, UGeometryIntersect.PointPlaneDistance(_position, new GPlane(_normal, 0))) { }
    }
    [Serializable]
    public struct GCone
    {
        public Vector3 origin;
        public Vector3 normal;
        [Range(0, 180)] public float angle;
        public GCone(Vector3 _origin, Vector3 _normal, float _angle) { origin = _origin;normal = _normal;angle = _angle; }
        public float GetRadius(float _height) => _height * Mathf.Tan(angle*UMath.Rad2Deg);
    }

    [Serializable]
    public struct GHeightCone
    {
        public Vector3 origin;
        public Vector3 normal;
        [Range(0,180)]public float angle;
        public float height;
        public float Radius => ((GCone)this).GetRadius(height);
        public Vector3 Bottom => origin + normal * height;
        public GHeightCone(Vector3 _origin, Vector3 _normal, float _radin, float _height)
        {
            origin = _origin;
            normal =_normal;
            angle = _radin;
            height = _height;
        }
        public static implicit operator GCone(GHeightCone _cone)=>new GCone(_cone.origin,_cone.normal,_cone.angle);
    }
    #endregion
    
}