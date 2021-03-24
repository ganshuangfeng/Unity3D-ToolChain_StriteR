﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UCommon
{
    public static bool InRange(this RangeFloat _value, float _check) => _value.start <= _check && _check <= _value.end;
    public static float InRangeScale(this RangeFloat _value, float _check) => Mathf.InverseLerp(_value.start, _value.end, _check);
    public static void TraversalEnum<T>(Action<T> enumAction) where T:Enum 
    {
        foreach (object temp in Enum.GetValues(typeof(T)))
        {
            if (temp.ToString() == "Invalid")
                continue;
            enumAction((T)temp);
        }
    }
    public static List<T> GetEnumList<T>() where T:Enum
    {
        List<T> list = new List<T>();
        Array allEnums = Enum.GetValues(typeof(T));
        for (int i = 0; i < allEnums.Length; i++)
        {
            if (allEnums.GetValue(i).ToString() == "Invalid")
                continue;
            list.Add((T)allEnums.GetValue(i));
        }
        return list;
    }
    public static bool IsFlagEnable<T>(this T _flag,T _compare) where T:Enum
    {
        int srcFlag = Convert.ToInt32(_flag);
        int compareFlag = Convert.ToInt32(_compare);
        return (srcFlag&compareFlag)== compareFlag;
    }
    public static bool IsFlagClear<T>(this T _flag) where T : Enum => Convert.ToInt32(_flag) == 0;
    public static IEnumerable<bool> GetNumerable<T>(this T _flags) where T:Enum
    {
        int flagValues =Convert.ToInt32(_flags);
        int maxPower=Convert.ToInt32( Enum.GetValues(typeof(T)).Cast<T>().Max());
        for(int i=0;i<32 ;i++ )
        {
            int curPower = UMath.Power(2,i);
            if (curPower > maxPower)
                yield break;
            yield return (flagValues&curPower)==curPower;
        }
        yield break;
    }
}

public static class UUnityEngine
{
    public static bool SetActive(this Transform _transform, bool _active) => SetActive(_transform.gameObject, _active);
    public static bool SetActive(this MonoBehaviour _monobehaviour, bool _active) => SetActive(_monobehaviour.gameObject, _active);
    public static bool SetActive(this GameObject _transform, bool _active)
    {
        if (_transform.activeSelf == _active)
            return false;

        _transform.SetActive(_active);
        return true;
    }

    #region Transform
    public static void DestroyChildren(this Transform trans)
    {
        int count = trans.childCount;
        if (count <= 0)
            return;
        for (int i = 0; i < count; i++)
            GameObject.Destroy(trans.GetChild(i).gameObject);
    }
    public static void SetParentResetTransform(this Transform source, Transform target)
    {
        source.SetParent(target);
        source.transform.localPosition = Vector3.zero;
        source.transform.localScale = Vector3.one;
        source.transform.localRotation = Quaternion.identity;
    }
    public static void SetChildLayer(this Transform trans, int layer)
    {
        foreach (Transform temp in trans.gameObject.GetComponentsInChildren<Transform>(true))
            temp.gameObject.layer = layer;
    }
    public static Transform FindInAllChild(this Transform trans, string name)
    {
        foreach (Transform temp in trans.gameObject.GetComponentsInChildren<Transform>(true))
            if (temp.name == name) return temp;
        Debug.LogWarning("Null Child Name:" + name + ",Find Of Parent:" + trans.name);
        return null;
    }

    public static T Find<T>(this T[,] array, Predicate<T> predicate)
    {
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        for (int i = 0; i < length0; i++)
            for (int j = 0; j < length1; j++)
                if (predicate(array[i, j])) return array[i, j];
        return default(T);
    }


    public static void SortChildByNameIndex(Transform transform, bool higherUpper = true)
    {
        List<Transform> childList = new List<Transform>();
        List<int> childIndexList = new List<int>();

        for (int i = 0; i < transform.childCount; i++)
        {
            childList.Add(transform.GetChild(i));
            childIndexList.Add(int.Parse(childList[i].gameObject.name));
        }
        childIndexList.Sort((a, b) => { return a <= b ? (higherUpper ? 1 : -1) : (higherUpper ? -1 : 1); });

        for (int i = 0; i < childList.Count; i++)
        {
            childList[i].SetSiblingIndex(childIndexList.FindIndex(p => p == int.Parse(childList[i].name)));
        }
    }
    #endregion

    public static Rect Reposition(this Rect _rect, float _newPositionX, float _newPositionY) => Reposition(_rect, new Vector2(_newPositionX, _newPositionY));
    public static Rect Reposition(this Rect _rect, Vector2 _newPosition) { _rect.position = _newPosition; return _rect; }
    public static Rect Resize(this Rect _rect, float _newSizeX, float _newSizeY) => Resize(_rect, new Vector2(_newSizeX, _newSizeY));
    public static Rect Resize(this Rect _rect, Vector2 _newSize) { _rect.size = _newSize; return _rect; }
    public static Vector3 GetPoint(this Bounds _bound, Vector3 _normalizedSize) => _bound.center + _bound.size.Multiply(_normalizedSize);

    #region Camera Helper
    public static bool InputRayCheck(this Camera _camera, Vector2 _inputPos, out RaycastHit _hit, int _layerMask = -1)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            _hit = new RaycastHit();
            return false;
        }

        return Physics.Raycast(_camera.ScreenPointToRay(_inputPos), out _hit, 1000, _layerMask);
    }
    public static Quaternion CameraProjectionOnPlane(this Camera _camera, Vector3 _position) => Quaternion.LookRotation(Vector3.ProjectOnPlane(_position - _camera.transform.position, _camera.transform.right), _camera.transform.up);
    #endregion
}

public static class URender
{
    public static MeshPolygon[] GetPolygons(int[] _indices)
    {
        MeshPolygon[] polygons = new MeshPolygon[_indices.Length / 3];
        for (int i = 0; i < polygons.Length; i++)
        {
            int startIndex = i * 3;
            int triangle0 = _indices[startIndex];
            int triangle1 = _indices[startIndex + 1];
            int triangle2 = _indices[startIndex + 2];
            polygons[i] = new MeshPolygon(triangle0, triangle1, triangle2);
        }
        return polygons;
    }
    public static MeshPolygon[] GetPolygons(this Mesh _srcMesh,out int[] _indices)
    {
        _indices = _srcMesh.triangles;
        return GetPolygons(_indices);
    }
    public static void TraversalBlendShapes(this Mesh _srcMesh,Action<string,int,int,float,Vector3[],Vector3[],Vector3[]> _OnEachFrame)
    {
        Vector3[] deltaVerticies=null;
        Vector3[] deltaNormals=null;
        Vector3[] deltaTangents = null; 
        int totalBlendshapes = _srcMesh.blendShapeCount;
        for(int i=0;i<_srcMesh.blendShapeCount;i++)
        {
            int frameCount = _srcMesh.GetBlendShapeFrameCount(i);
            string name = _srcMesh.GetBlendShapeName(i);
            for(int j=0;j<frameCount;j++)
            {
                float weight = _srcMesh.GetBlendShapeFrameWeight(i, j);
                _srcMesh.GetBlendShapeFrameVertices(i,j,deltaVerticies,deltaNormals,deltaTangents);
                _OnEachFrame(name,i,j,weight,deltaVerticies,deltaNormals,deltaTangents);
            }
        }
    }
    public static bool GetVertexData(this Mesh _srcMesh,enum_VertexData _dataType, List<Vector4> vertexData)
    {
        vertexData.Clear();
        switch(_dataType)
        {
            default:throw new Exception("Invalid Vertex Data Type"+_dataType);
            case enum_VertexData.UV0:
            case enum_VertexData.UV1:
            case enum_VertexData.UV2:
            case enum_VertexData.UV3:
            case enum_VertexData.UV4:
            case enum_VertexData.UV5:
            case enum_VertexData.UV6:
            case enum_VertexData.UV7:
                _srcMesh.GetUVs((int)_dataType, vertexData);
                break;
            case enum_VertexData.Tangent:
                _srcMesh.GetTangents(vertexData);
                break;
            case enum_VertexData.Normal:
                {
                    List<Vector3> normalList = new List<Vector3>();
                    _srcMesh.GetNormals(normalList);
                    foreach (var normal in normalList)
                        vertexData.Add(normal);
                }
                break;
        }
        return vertexData!=null;
    }
    public static void SetVertexData(this Mesh _srcMesh,enum_VertexData _dataType,List<Vector4> _data)
    {
        switch (_dataType)
        {
            default: throw new Exception("Invalid Vertex Data Type" + _dataType);
            case enum_VertexData.UV0:
            case enum_VertexData.UV1:
            case enum_VertexData.UV2:
            case enum_VertexData.UV3:
            case enum_VertexData.UV4:
            case enum_VertexData.UV5:
            case enum_VertexData.UV6:
            case enum_VertexData.UV7:
                _srcMesh.SetUVs((int)_dataType, _data);
                break;
            case enum_VertexData.Tangent:
                _srcMesh.SetTangents(_data);
                break;
            case enum_VertexData.Normal:
                    _srcMesh.SetNormals(_data.ToArray(vec4=>vec4.ToVector3()));
                break;
        }
    }
    public static bool GetVertexData(this Mesh _srcMesh, enum_VertexData _dataType, List<Vector3> vertexData)
    {
        vertexData.Clear();
        switch (_dataType)
        {
            default: throw new Exception("Invalid Vertex Data Type" + _dataType);
            case enum_VertexData.UV0:
            case enum_VertexData.UV1:
            case enum_VertexData.UV2:
            case enum_VertexData.UV3:
            case enum_VertexData.UV4:
            case enum_VertexData.UV5:
            case enum_VertexData.UV6:
            case enum_VertexData.UV7:
                _srcMesh.GetUVs((int)_dataType, vertexData);
                break;
            case enum_VertexData.Tangent:
                List<Vector4> tangents = new List<Vector4>();
                _srcMesh.GetTangents(tangents);
                foreach (var tangent in tangents)
                    vertexData.Add(tangent.ToVector3());
                break;
            case enum_VertexData.Normal:
                {
                    List<Vector3> normalList = new List<Vector3>();
                    _srcMesh.GetNormals(normalList);
                    foreach (var normal in normalList)
                        vertexData.Add(normal);
                }
                break;
        }
        return vertexData != null;
    }
    public static void SetVertexData(this Mesh _srcMesh, enum_VertexData _dataType, List<Vector3> _data)
    {
        switch (_dataType)
        {
            default: throw new Exception("Invalid Vertex Data Type" + _dataType);
            case enum_VertexData.UV0:
            case enum_VertexData.UV1:
            case enum_VertexData.UV2:
            case enum_VertexData.UV3:
            case enum_VertexData.UV4:
            case enum_VertexData.UV5:
            case enum_VertexData.UV6:
            case enum_VertexData.UV7:
                _srcMesh.SetUVs((int)_dataType, _data);
                break;
            case enum_VertexData.Tangent:
                _srcMesh.SetTangents(_data.ToArray(p=>p.ToVector4(1f)));
                break;
            case enum_VertexData.Normal:
                _srcMesh.SetNormals(_data);
                break;
        }
    }

    public static void EnableKeyword(this Material _material, string _keyword, bool _enable)
    {
        if (_enable)
            _material.EnableKeyword(_keyword);
        else
            _material.DisableKeyword(_keyword);
    }
    public static void EnableKeywords(this Material _material, string[] _keywords, int _target)
    {
        for (int i = 0; i < _keywords.Length; i++)
            _material.EnableKeyword(_keywords[i], i + 1 == _target);
    }
    public static void EnableGlobalKeyword(string[] _keywords, int _target)
    {
        for (int i = 0; i < _keywords.Length; i++)
            EnableGlobalKeyword(_keywords[i], (i + 1) == _target);
    }

    public static void EnableGlobalKeyword(string _keyword, bool _enable)
    {
        if (_enable)
            Shader.EnableKeyword(_keyword);
        else
            Shader.DisableKeyword(_keyword);
    }

    public static Material CreateMaterial(Type _type)
    {
        Shader _shader = Shader.Find("Hidden/" + _type.Name);

        if (_shader == null)
            throw new NullReferenceException("Invalid ImageEffect Shader Found:" + _type.Name);

        if (!_shader.isSupported)
            throw new NullReferenceException("Shader Not Supported:" + _type.Name);

        return new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
    }
}