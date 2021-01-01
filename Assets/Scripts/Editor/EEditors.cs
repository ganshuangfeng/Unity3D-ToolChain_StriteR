﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshFilter)),CanEditMultipleObjects]
public class MeshFilterEditor: Editor
{
    MeshFilter m_Target;
    bool m_EnableVertexDataVisualize;
    bool m_DrawVertex = true;
    Color m_VertexColor = Color.white;
    enum_UVType m_UVIndex=0;
    float m_UVsLength=.5f;

    bool m_DrawNormals = false;
    float m_NormalsLength = .5f;
    Color m_NormalColor = Color.blue;
    bool m_DrawTangents = false;
    float m_TangentsLength = .5f;
    Color m_TangentColor = Color.green;
    bool m_DrawBiTangents = false;
    float m_BiTangentsLength = .5f;
    Color m_BitangentColor = Color.yellow;
    enum_ColorType m_DrawColorType;
    float m_ColorLength = .5f;

    List<Vector4> m_UVList = new List<Vector4>();
    void OnEnable()
    {
        m_Target = target as MeshFilter;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.BeginVertical();
        m_EnableVertexDataVisualize = EditorGUILayout.Foldout(m_EnableVertexDataVisualize, "Vertex Data Visualize");

        if (m_EnableVertexDataVisualize)
        {
            bool haveNormals = m_Target.sharedMesh.normals.Length>0;
            bool haveTangents = m_Target.sharedMesh.tangents.Length > 0;
            bool haveColors = m_Target.sharedMesh.colors.Length > 0;

            EditorGUILayout.BeginHorizontal();
            m_DrawVertex=EditorGUILayout.Toggle("Draw Vertex",m_DrawVertex);
            m_VertexColor = EditorGUILayout.ColorField(m_VertexColor);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(); 
            if(haveNormals)
            {
                m_DrawNormals = EditorGUILayout.Toggle("Draw Normal", m_DrawNormals);
                m_NormalColor = EditorGUILayout.ColorField(m_NormalColor);
                m_NormalsLength = EditorGUILayout.Slider(m_NormalsLength, 0f, 2f);
            }
            else
            {
                m_DrawNormals = false;
                EditorGUILayout.LabelField("No Normals Data");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (haveTangents)
            {
                m_DrawTangents = EditorGUILayout.Toggle("Draw Tangents", m_DrawTangents);
                m_TangentColor = EditorGUILayout.ColorField(m_TangentColor);
                m_TangentsLength = EditorGUILayout.Slider(m_TangentsLength, 0f, 2f);
            }
            else
            {
                m_DrawTangents = false;
                EditorGUILayout.LabelField("No Tangents Data");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if(haveNormals&&haveTangents)
            {
                m_DrawBiTangents = EditorGUILayout.Toggle("Draw Bi-Tangents", m_DrawBiTangents);
                m_BitangentColor = EditorGUILayout.ColorField(m_BitangentColor);
                m_BiTangentsLength = EditorGUILayout.Slider(m_BiTangentsLength, 0f, 2f);
            }
            else
            {
                m_DrawBiTangents = false;
                EditorGUILayout.LabelField("Unable To Calculate Bi Tangents");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if(haveColors)
            {
                m_DrawColorType = (enum_ColorType)EditorGUILayout.EnumPopup("Draw Color", m_DrawColorType);
                m_ColorLength = EditorGUILayout.Slider(m_ColorLength, 0f, 2f);
            }
            else
            {
                m_DrawColorType = enum_ColorType.None;
                EditorGUILayout.LabelField("No Color Data");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_UVIndex = (enum_UVType)EditorGUILayout.EnumPopup("Draw UV", m_UVIndex);
            if(m_UVIndex != enum_UVType.None)
            {
                GetUVs(m_Target.sharedMesh, m_UVIndex, m_UVList);
                if (m_UVList.Count != 0)
                    m_UVsLength = EditorGUILayout.Slider(m_UVsLength, 0f, 2f);
                else
                    EditorGUILayout.LabelField("No UV Data");
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
    private void OnSceneGUI()
    {
        if (!m_Target||!m_EnableVertexDataVisualize)
            return;

        Handles.matrix = m_Target.transform.localToWorldMatrix;
        Vector3[] verticies = m_Target.sharedMesh.vertices;
        Vector3[] normals = m_Target.sharedMesh.normals;
        Vector4[] tangents = m_Target.sharedMesh.tangents;
        Color[] colors = m_Target.sharedMesh.colors;
        GetUVs(m_Target.sharedMesh, m_UVIndex,m_UVList);
        int[] indices = m_Target.sharedMesh.GetIndices(0);
        if (m_DrawVertex)
        {
            Handles.color = m_VertexColor;
            int triangleCount = indices.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                int startIndex = i * 3;
                Handles.DrawLine(verticies[indices[startIndex]], verticies[indices[startIndex + 1]]);
                Handles.DrawLine(verticies[indices[startIndex + 1]], verticies[indices[startIndex + 2]]);
                Handles.DrawLine(verticies[indices[startIndex + 2]], verticies[indices[startIndex]]);
            }
        }


        for (int i = 0; i < verticies.Length; i++)
        {
            if (m_DrawNormals)
            {
                Handles.color = Color.blue;
                Handles.DrawLine(verticies[i], verticies[i] + normals[i] * m_NormalsLength);
            }

            if (m_DrawTangents)
            {
                Handles.color = Color.green;
                Handles.DrawLine(verticies[i], verticies[i] + new Vector3(tangents[i].x, tangents[i].y, tangents[i].z) * tangents[i].w * m_TangentsLength);
            }

            if (m_DrawBiTangents)
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(verticies[i], verticies[i] + Vector3.Cross(normals[i], tangents[i]).normalized * m_BiTangentsLength);
            }

            if (m_UVList .Count!= 0)
            {
                Handles.color = m_UVList[i].ToColor().SetAlpha(1f);
                Handles.DrawLine(verticies[i], verticies[i] + normals[i] * m_UVsLength);
            }

            if (m_DrawColorType!= enum_ColorType.None)
            {
                Color vertexColor = Color.clear;

                switch (m_DrawColorType)
                {
                    case enum_ColorType.RGBA: vertexColor = colors[i]; break;
                    case enum_ColorType.R:vertexColor = Color.red * colors[i].r; ; break;
                    case enum_ColorType.G: vertexColor =Color.green* colors[i].g; break;
                    case enum_ColorType.B: vertexColor = Color.blue* colors[i].b; break;
                    case enum_ColorType.A:vertexColor = Color.white * colors[i].a; break;
                }
                Handles.color = vertexColor;
                Handles.DrawLine(verticies[i], verticies[i] + normals[i] * m_ColorLength);
            }
        }
    }

    enum enum_ColorType
    {
        None,
        RGBA,
        R,
        G,
        B,
        A,
    }
    enum enum_UVType
    {
        None,
        UV1,
        UV2,
        UV3,
        UV4,
        UV5,
        UV6,
        UV7,
        UV8,
    }

    void GetUVs(Mesh _target,enum_UVType _index,List<Vector4> uvList)
    {
        uvList.Clear();
        switch (_index)
        {
            default:break;
            case enum_UVType.UV1:  _target.GetUVs(0,uvList);break;
            case enum_UVType.UV2: _target.GetUVs(1, uvList); break;
            case enum_UVType.UV3: _target.GetUVs(2, uvList); break;
            case enum_UVType.UV4: _target.GetUVs(3, uvList); break;
            case enum_UVType.UV5: _target.GetUVs(4, uvList); break;
            case enum_UVType.UV6: _target.GetUVs(5, uvList); break;
            case enum_UVType.UV7: _target.GetUVs(6, uvList); break;
            case enum_UVType.UV8: _target.GetUVs(7, uvList); break;
        }
    }
    
}