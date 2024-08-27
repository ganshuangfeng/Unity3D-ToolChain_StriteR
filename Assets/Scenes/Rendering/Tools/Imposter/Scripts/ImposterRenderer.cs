﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Extensions;
using Runtime;
using Runtime.Geometry;
using Runtime.Geometry.Explicit.Sphere;
using Unity.Mathematics;
using UnityEngine;

namespace Examples.Rendering.Imposter
{
    [Serializable]
    public class ImposterRendererCore : ARuntimeRendererBase
    {
        [ScriptableObjectEdit(true)] public ImposterData m_Data;
        public override bool isBillboard() => true;
        private MeshRenderer m_Renderer;
        private static int kInstanceID = 0;
        protected override string GetInstanceName() => $"Imposter - {kInstanceID++}";

        public override Mesh Initialize(Transform _transform)
        {
            m_Renderer = _transform.GetComponent<MeshRenderer>();
            return base.Initialize(_transform);
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if(m_Renderer!=null && m_Data != null)
                m_Renderer.sharedMaterial = m_Data.m_Material;
        }

        private static List<Vector3> kVertices = new List<Vector3>();
        private static List<int> kIndices = new List<int>();
        private static List<Vector4> kUVs = new List<Vector4>();
        protected override void PopulateMesh(Mesh _mesh, Transform _transform, Transform _viewTransform)
        {
            if (!m_Data)
                return;
            
            var center = _transform.TransformPoint(m_Data.m_BoundingSphere.center);
            var viewDirectionWS = (_viewTransform.position - center).normalized;
            var viewDirectionOS = _transform.worldToLocalMatrix.rotation * viewDirectionWS;
            
            var weights = float4.zero;
            var size = m_Data.m_BoundingSphere.radius;
            var block = new MaterialPropertyBlock();
            var axis = GAxis.kDefault;
            if (m_Data.m_Parallax <= 0)
            {
                var corner = m_Data.m_Input.GetImposterCorner(viewDirectionOS);
                axis = GAxis.ForwardBillboard(0,-corner.direction);
                math.sincos(0,out var s0,out var c0);
                var X = size * c0 * axis.right + size * s0 * axis.up;
                var Y = -size * s0 * axis.right + size * c0 * axis.up;
                var billboard = new GQuad(-X - Y,-X + Y, X + Y,X - Y) + m_Data.m_BoundingSphere.center;
                weights[0] = 1;
                
                _mesh.SetVertices(billboard.Select(p=>(Vector3)p).FillList(kVertices));
                _mesh.SetIndices(PQuad.kDefault.GetTriangleIndexes().FillList(kIndices),MeshTopology.Triangles,0);
                var texelSize = corner.uvRect.ToTexelSize();
                _mesh.SetUVs(0,G2Quad.kDefaultUV.Select(p=> (Vector4)(URender.TransformTex(p ,texelSize)).to4()).FillList(kUVs));
            }
            else
            {
                var output = m_Data.m_Input.GetImposterViews(viewDirectionOS);
                weights = output.weights;
                var centerOS = output.centroid;
                axis = GAxis.ForwardBillboard(0,-centerOS);
                math.sincos(0,out var s0,out var c0);
                var X = size * c0 * axis.right + size * s0 * axis.up;
                var Y = -size * s0 * axis.right + size * c0 * axis.up;
                var billboard = new GQuad(-X - Y,-X + Y, X + Y,X - Y) + m_Data.m_BoundingSphere.center;
                
                _mesh.SetVertices(billboard.Select(p=>(Vector3)p).FillList(kVertices));
                _mesh.SetIndices(PQuad.kDefault.GetTriangleIndexes().FillList(kIndices),MeshTopology.Triangles,0);
                
                for (var texelIndex = 0; texelIndex < 4; texelIndex++)
                {
                    var weight = output.weights[texelIndex];
                    if ( weight == 0 )
                        continue;
                    
                    var corner = m_Data.m_Input.GetImposterCorner(output.corners[texelIndex]);
                    var parallax = -axis.GetUV(corner.direction) * m_Data.m_Parallax * 2;
                    var texelST = corner.uvRect.ToTexelSize();
                    _mesh.SetUVs(texelIndex,G2Quad.kDefaultUV.Select(p=>(Vector4)(URender.TransformTex(p  ,texelST)+ parallax * m_Data.m_Input.CellTexelSizeNormalized).to4()).FillList(kUVs));
                }
            }
            
            block.SetVector(ImposterShaderProperties.kWeights,weights);
            block.SetVector(ImposterShaderProperties.kBoundingID,(float4)m_Data.m_BoundingSphere);
            block.SetVector(ImposterShaderProperties.kRotation,((quaternion)_transform.rotation).value);
            block.SetVector("_ImposterViewDirection",axis.forward.to4());
            
            m_Renderer.SetPropertyBlock(block);
        }

        public bool m_DrawInput;
        public override void DrawGizmos(Transform _transform,Transform _viewTransform)
        {
            base.DrawGizmos(_transform,_viewTransform);
            if (m_Data == null)
                return;

            var center = _transform.TransformPoint(m_Data.m_BoundingSphere.center);

            Gizmos.matrix = Matrix4x4.TRS(center,Quaternion.identity,_transform.lossyScale * m_Data.m_BoundingSphere.radius);
            var viewDirection = _transform.worldToLocalMatrix.rotation * (_viewTransform.position - center).normalized;
            if(m_DrawInput)
                m_Data.m_Input.DrawGizmos(_viewTransform.position);
            
            var output = m_Data.m_Input.GetImposterViews(viewDirection);

            var uv = m_Data.m_Input.mapping.SphereToUV(viewDirection);
            Gizmos.color = Color.blue;
            UGizmos.DrawString(viewDirection,(uv *(int)m_Data.m_Input.count).ToString(),0.02f);
            Gizmos.DrawWireSphere(output.centroid,.01f);
            Gizmos.DrawCube(m_Data.m_Input.mapping.UVToSphere(uv), Vector3.one * 0.01f);
            for(var i = 0 ; i < 4 ; i++)
            {
                var weight = output.weights[i];
                var corner = m_Data.m_Input.GetImposterCorner(output.corners[i]);
                Gizmos.color =  UColor.IndexToColor(i).SetA(weight);
                Gizmos.DrawCube( corner.direction, Vector3.one * 0.02f);
                UGizmos.DrawString(corner.direction,corner.cellIndex.ToString(),0.04f);
            }
        }
    }
    
    public class ImposterRenderer : ARuntimeRendererMonoBehaviour<ImposterRendererCore>
    {
    }
}