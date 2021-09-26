using Geometry;
using TPoolStatic;
using UnityEngine;

namespace ConvexGrid
{
    public class ModuleBakerModel : MonoBehaviour
    {
        public Qube<bool> m_Relation;
        public OrientedModuleMeshData CollectModuleMesh()
        {
            var vertices = TSPoolList<Vector3>.Spawn();
            var indexes = TSPoolList<int>.Spawn();
            var uvs = TSPoolList<Vector2>.Spawn();
            var normals = TSPoolList<Vector3>.Spawn();

            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
            {
                var mesh = meshFilter.sharedMesh;
                var localToWorldMatrix = meshFilter.transform.localToWorldMatrix;
                var worldToLocalMatrix = transform.worldToLocalMatrix;

                int indexOffset = vertices.Count;

                var curVertices = mesh.vertices;
                var curNormals = mesh.normals;
                for (int i = 0; i < curVertices.Length; i++)
                {
                    var positionWS = localToWorldMatrix.MultiplyPoint(curVertices[i]);
                    var positionOS = worldToLocalMatrix.MultiplyPoint(positionWS);
                    var normalWS = localToWorldMatrix.MultiplyVector(curNormals[i]);
                    var normalOS = worldToLocalMatrix.MultiplyVector(normalWS);
                    vertices.Add(UModule.ObjectToModuleVertex(positionOS));
                    normals.Add(normalOS);
                }

                foreach (var index in mesh.GetIndices(0))
                    indexes.Add(indexOffset+index);    
                
                uvs.AddRange(mesh.uv);
            }
            
            var moduleMesh=new OrientedModuleMeshData
            {
                m_Vertices = vertices.ToArray(),
                m_UVs=uvs.ToArray(),
                m_Indexes = indexes.ToArray(),
                m_Normals = normals.ToArray(),
            };
            
            TSPoolList<Vector3>.Recycle(vertices);
            TSPoolList<int>.Recycle(indexes);
            TSPoolList<Vector2>.Recycle(uvs);
            TSPoolList<Vector3>.Recycle(normals);
            return moduleMesh;
        }
        
        public void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up*.5f,Vector3.one);
            for (int i = 0; i < 8; i++)
            {
                Gizmos.color = m_Relation[i] ? Color.green : Color.red.SetAlpha(.5f);
                Gizmos.DrawWireSphere(UModule.unitQube[i],.1f);
            }
        }
    }

}