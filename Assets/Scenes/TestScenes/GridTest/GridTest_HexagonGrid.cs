using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Procedural;
using Procedural.Hexagon;
using Procedural.Hexagon.Area;

namespace GridTest
{
    [ExecuteInEditMode]
    public class GridTest_HexagonGrid : MonoBehaviour
    {
        public bool m_Flat = false;
        public float m_CellRadius = 1;
        [Header("Area")]
        public int m_AreaRadius = 8;
        public int m_Tilling = 1;
        public bool m_Welded = false;
        public int m_MaxAreaRadius = 4;
#if UNITY_EDITOR
        private Coord m_HitPointCS;
        private PHexAxial m_HitAxialCS;

        public enum EAxisVisualize
        {
            Invalid,
            Axial,
            Cube,
        }

        [NonSerialized]
        private readonly Dictionary<PHexCube, HexagonArea> m_Areas = new Dictionary<PHexCube, HexagonArea>();

        private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnValidate() => Clear();

        void Clear()
        {
            m_Areas.Clear();
        }

        private void OnDrawGizmos()
        {
            UHexagon.flat = m_Flat;
            UHexagonArea.Init(m_AreaRadius, m_Tilling,m_Welded);
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * m_CellRadius);

            foreach (var coord in UHexagon.GetCoordsInRadius( PHexCube.zero,50))
            {
                var area = UHexagonArea.GetBelongingArea(coord);

                var index = (area.coord.x-area.coord.y + int.MaxValue / 2) % 3;
                // var index = (i - k + int.MaxValue) % 3;
                switch (index)
                {
                    case 0: Gizmos.color = Color.red; break;
                    case 1: Gizmos.color = Color.blue; break;
                    case 2:  Gizmos.color = Color.green; break;
                }
                coord.DrawHexagon();
            }
            
            DrawAxis();
            DrawAreas();
            DrawTestGrids(m_HitPointCS, m_HitAxialCS);
        }

        void ValidateArea(PHexCube _positionCS,bool _include)
        {
            var area = UHexagonArea.GetBelongingArea(_positionCS);

            if (!area.coord.InRange(m_MaxAreaRadius))
                return;
           
            if(_include&&!m_Areas.ContainsKey(area.coord))
                m_Areas.Add(area.coord, area);
            else if (!_include && m_Areas.ContainsKey(area.coord))
                m_Areas.Remove(area.coord);
        }
        


        public EAxisVisualize m_AxisVisualize;

        private static class GUIHelper
        {
            public static readonly Color C_AxialColumn = Color.green;
            public static readonly Color C_AxialRow = Color.blue;
            public static readonly Color C_CubeX = Color.red;
            public static readonly Color C_CubeY = Color.green;
            public static readonly Color C_CubeZ = Color.blue;

            public static readonly GUIStyle m_AreaStyle = new GUIStyle
                {alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Normal};

            public static readonly GUIStyle m_HitStyle = new GUIStyle
                {alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Normal};
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            GRay ray = sceneView.camera.ScreenPointToRay(TEditor.UECommon.GetScreenPoint(sceneView));
            GPlane plane = new GPlane(Vector3.up, transform.position);
            var hitPoint = ray.GetPoint(UGeometry.RayPlaneDistance(plane, ray));
            m_HitPointCS = (transform.InverseTransformPoint(hitPoint) / m_CellRadius).ToPixel();
            m_HitAxialCS = m_HitPointCS.ToAxial();
            if (Event.current.type == EventType.MouseDown)
                switch (Event.current.button)
                {
                    case 0:
                        ValidateArea(m_HitAxialCS,true);
                        break;
                    case 1:
                        ValidateArea(m_HitAxialCS,false);
                        break;
                }


            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.R:
                        Clear();
                        break;
                    case KeyCode.F1:
                        m_Flat = !m_Flat;
                        break;
                    case KeyCode.F2:
                        m_AxisVisualize = m_AxisVisualize.Next();
                        break;
                }
            }

            DrawSceneHandles();
        }

        void DrawSceneHandles()
        {
            Handles.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * m_CellRadius);
            foreach (var hex in m_Areas.Values)
                Handles.Label(hex.centerCS.ToAxial().ToPixel().ToWorld(), $"A:{hex.coord}\nC:{hex.centerCS}",
                    GUIHelper.m_AreaStyle);
            var area = UHexagonArea.GetBelongingArea(m_HitAxialCS);
            Handles.Label(m_HitPointCS.ToWorld(),
                $"Cell:{m_HitAxialCS}\nArea:{area.coord}\nAPos{area.TransformCSToAS(m_HitAxialCS)}",
                GUIHelper.m_HitStyle);
        }

        void DrawAreas()
        {
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * m_CellRadius);
            Gizmos.color = Color.grey;
            
            foreach (var area in m_Areas)
            foreach (var coordsCS in UHexagonArea.IterateAllCoordsCS(area.Value))
                coordsCS.DrawHexagon();
            
            Gizmos.color = Color.white;
            foreach (var area in m_Areas)
            foreach (var coordsCS in UHexagonArea.IterateAllCoordsCSRinged(area.Value))
                coordsCS.coord.DrawHexagon();

            Gizmos.color = Color.cyan;
                foreach (var area in m_Areas.Values)
                    area.centerCS.DrawHexagon();
        }

        void DrawAxis()
        {
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * m_CellRadius) *
                            Matrix4x4.Translate(m_HitAxialCS.ToPixel().ToWorld());
            switch (m_AxisVisualize)
            {
                default: return;
                case EAxisVisualize.Axial:
                {
                    Gizmos.color = GUIHelper.C_AxialColumn;
                    Gizmos.DrawRay(Vector3.zero, new PHexAxial(1, 0).ToPixel().ToWorld());
                    Gizmos.color = GUIHelper.C_AxialRow;
                    Gizmos.DrawRay(Vector3.zero, new PHexAxial(0, 1).ToPixel().ToWorld());
                }
                    break;
                case EAxisVisualize.Cube:
                {
                    Gizmos.color = GUIHelper.C_CubeX;
                    Gizmos.DrawRay(Vector3.zero, new PHexAxial(1, 0).ToPixel().ToWorld());
                    Gizmos.color = GUIHelper.C_CubeY;
                    Gizmos.DrawRay(Vector3.zero, new PHexAxial(1, -1).ToPixel().ToWorld());
                    Gizmos.color = GUIHelper.C_CubeZ;
                    Gizmos.DrawRay(Vector3.zero, new PHexAxial(0, 1).ToPixel().ToWorld());
                }
                    break;
            }
        }

        public EGridAxialTest m_Test = EGridAxialTest.AxialAxis;

        [MFoldout(nameof(m_Test), EGridAxialTest.Range, EGridAxialTest.Intersect, EGridAxialTest.Distance,
            EGridAxialTest.Ring)]
        [Range(1, 5)]
        public int m_Radius1;

        [MFoldout(nameof(m_Test), EGridAxialTest.Intersect, EGridAxialTest.Distance)]
        public PHexAxial m_TestAxialPoint = new PHexAxial(2, 1);

        [MFoldout(nameof(m_Test), EGridAxialTest.Intersect)]
        public int m_Radius2;

        [MFoldout(nameof(m_Test), EGridAxialTest.Reflect)]
        public ECubeAxis m_ReflectAxis = ECubeAxis.X;

        public enum EGridAxialTest
        {
            Hit,
            AxialAxis,
            Range,
            Intersect,
            Distance,
            Nearby,
            Mirror,
            Reflect,
            Ring,
        }

        void DrawTestGrids(Coord hitPixel, PHexAxial hitAxial)
        {
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * m_CellRadius);
            Gizmos.DrawRay(hitPixel.ToWorld(), Vector3.up);
            switch (m_Test)
            {
                case EGridAxialTest.Hit:
                {
                    Gizmos.color = Color.yellow;
                    hitAxial.DrawHexagon();
                }
                    break;
                case EGridAxialTest.AxialAxis:
                {
                    Gizmos.color = Color.green;

                    var colPixel = hitPixel.SetCol(0);
                    var colAxis = colPixel.ToAxial();
                    var rowPixel = hitPixel.SetRow(0);
                    var rowAxis = rowPixel.ToAxial();
                    Gizmos.color = GUIHelper.C_AxialColumn;
                    Gizmos.DrawRay(colPixel.ToWorld(), Vector3.up);
                    Gizmos.DrawLine(Vector3.zero, colPixel.ToWorld());
                    Gizmos.DrawLine(colPixel.ToWorld(), hitPixel.ToWorld());
                    colAxis.DrawHexagon();
                    Gizmos.color = GUIHelper.C_AxialRow;
                    Gizmos.DrawRay(rowPixel.ToWorld(), Vector3.up);
                    Gizmos.DrawLine(Vector3.zero, rowPixel.ToWorld());
                    rowAxis.DrawHexagon();
                    Gizmos.DrawLine(rowPixel.ToWorld(), hitPixel.ToWorld());
                }
                    break;
                case EGridAxialTest.Range:
                {
                    Gizmos.color = Color.yellow;
                    foreach (PHexAxial axialPoint in hitAxial.GetCoordsInRadius(m_Radius1))
                        axialPoint.DrawHexagon();
                }
                    break;
                case EGridAxialTest.Intersect:
                {
                    foreach (PHexAxial axialPoint in hitAxial.GetCoordsInRadius(m_Radius1)
                        .Extend(m_TestAxialPoint.GetCoordsInRadius(m_Radius2)))
                    {
                        var offset1 = m_TestAxialPoint - axialPoint;
                        var offset2 = hitAxial - axialPoint;
                        bool inRange1 = offset1.InRange(m_Radius2);
                        bool inRange2 = offset2.InRange(m_Radius1);
                        if (inRange1 && inRange2)
                            Gizmos.color = Color.cyan;
                        else if (inRange1)
                            Gizmos.color = Color.green;
                        else if (inRange2)
                            Gizmos.color = Color.blue;
                        else
                            continue;

                        axialPoint.DrawHexagon();
                    }
                }
                    break;
                case EGridAxialTest.Distance:
                {
                    foreach (PHexAxial axialPoint in m_TestAxialPoint.GetCoordsInRadius(m_Radius1))
                    {
                        int offset = m_TestAxialPoint.Distance(axialPoint);
                        Gizmos.color = Color.Lerp(Color.green, Color.yellow, ((float) offset) / m_Radius1);
                        axialPoint.DrawHexagon();
                    }
                }
                    break;
                case EGridAxialTest.Nearby:
                {
                    foreach (var nearbyAxial in hitAxial.GetCoordsNearby().LoopIndex())
                    {
                        Gizmos.color = Color.Lerp(Color.blue, Color.red, nearbyAxial.index / 6f);
                        nearbyAxial.value.DrawHexagon();
                    }
                }
                    break;
                case EGridAxialTest.Mirror:
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Gizmos.color = Color.Lerp(Color.green, Color.red, ((float) i) / 6);
                        var axialOffset = UHexagon.RotateMirror(m_AreaRadius - 1, i).ToAxial();
                        var coords = hitAxial.GetCoordsInRadius(m_AreaRadius);
                        foreach (PHexAxial axialPoint in coords)
                            (axialPoint + axialOffset).DrawHexagon();
                    }
                }
                    break;
                case EGridAxialTest.Reflect:
                {
                    var axialHitCube = hitAxial.ToCube();
                    var reflectCube = axialHitCube.Reflect(m_ReflectAxis);
                    Gizmos.color = Color.yellow;
                    hitAxial.DrawHexagon();
                    Gizmos.color = Color.green;
                    reflectCube.ToAxial().DrawHexagon();
                    Gizmos.color = Color.blue;
                    (-axialHitCube).ToAxial().DrawHexagon();
                    Gizmos.color = Color.red;
                    (-reflectCube).ToAxial().DrawHexagon();
                }
                    break;
                case EGridAxialTest.Ring:
                {
                    foreach (var cubeCS in m_HitAxialCS.ToCube().GetCoordsRinged(m_Radius1))
                    {
                        Gizmos.color = Color.Lerp(Color.white, Color.yellow, cubeCS.dir / 5f);
                        cubeCS.coord.DrawHexagon();
                    }
                }
                    break;
            }
        }
        #endif
    }
    
}