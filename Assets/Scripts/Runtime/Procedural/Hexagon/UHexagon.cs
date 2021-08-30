using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Procedural.Hexagon
{
    #region Flat&Pointy
    using static HexagonConst;
    public static class HexagonConst
    {
        public static readonly float C_SQRT3 = Mathf.Sqrt(3);
        public static readonly float C_SQRT3Half = C_SQRT3 / 2f;
        public static readonly float C_Inv_SQRT3 = 1f / C_SQRT3;
    }

    public interface IHexagonShape
    {
        public Coord[] m_PointOffsets { get; }
        public Matrix2x2 m_AxialToPixel { get; }
        public Matrix2x2 m_PixelToAxial { get; }
    }


    internal sealed class UHexagonFlatHelper : IHexagonShape
    {
        private static readonly Matrix2x2 C_AxialToPixel_Flat = new Matrix2x2( 1.5f, 0f, C_SQRT3Half, C_SQRT3);
        private static readonly Matrix2x2 C_PixelToAxial_Flat = new Matrix2x2(2f / 3f, 0, -1f / 3f, C_Inv_SQRT3);

        private static readonly Coord[] C_FlatOffsets =
        {
            new Coord(1, 0), new Coord(.5f, -C_SQRT3Half),
            new Coord(-.5f, -C_SQRT3Half), new Coord(-1, 0),
            new Coord(-.5f, C_SQRT3Half), new Coord(.5f, C_SQRT3Half)
        };

        public Coord[] m_PointOffsets => C_FlatOffsets;
        public Matrix2x2 m_AxialToPixel => C_AxialToPixel_Flat;
        public Matrix2x2 m_PixelToAxial => C_PixelToAxial_Flat;
    }

    internal sealed class UHexagonPointyHelper : IHexagonShape
    {
        private static readonly Matrix2x2 C_AxialToPixel_Pointy = new Matrix2x2(C_SQRT3, C_SQRT3Half, 0, 1.5f);
        private static readonly Matrix2x2 C_PixelToAxial_Pointy = new Matrix2x2(C_Inv_SQRT3, -1f / 3f, 0f, 2f / 3f);

        private static readonly Coord[] C_PointyOffsets =
        {
            new Coord(0, 1), new Coord(C_SQRT3Half, .5f),
            new Coord(C_SQRT3Half, -.5f), new Coord(0, -1),
            new Coord(-C_SQRT3Half, -.5f), new Coord(-C_SQRT3Half, .5f)
        };

        public Coord[] m_PointOffsets => C_PointyOffsets;
        public Matrix2x2 m_AxialToPixel => C_AxialToPixel_Pointy;
        public Matrix2x2 m_PixelToAxial => C_PixelToAxial_Pointy;
    }
    #endregion

    public static class UHexagon
    {
        private static readonly UHexagonFlatHelper m_FlatHelper = new UHexagonFlatHelper();
        private static readonly UHexagonPointyHelper m_PointHelper = new UHexagonPointyHelper();
        static IHexagonShape m_Shaper = m_FlatHelper;

        public static bool flat
        {
            set { m_Shaper = value ? (IHexagonShape) m_FlatHelper : m_PointHelper; }
        }

        public static Coord[] GetHexagonPoints() => m_Shaper.m_PointOffsets;

        #region Transformation

        public static Coord ToPixel(this PHexOffset _offset, bool _flat)
        {
            if (_flat)
                return new Coord(_offset.col * 1.5f, C_SQRT3Half * (_offset.row * 2 + _offset.col % 2));
            return new Coord(C_SQRT3Half * (_offset.col * 2 + _offset.row % 2), _offset.row * 1.5f);
        }

        public static Coord ToPixel(this PHexAxial _axial) =>
            new Coord(m_Shaper.m_AxialToPixel.Multiply(_axial.col, _axial.row));

        public static Coord ToPixel(this PHexCube _cube) => _cube.ToAxial().ToPixel();
        public static PHexAxial ToAxial(this Coord pPoint) =>
            new PHexAxial(m_Shaper.m_PixelToAxial.Multiply(pPoint));

        public static PHexAxial ToAxial(this PHexCube _cube) => new PHexAxial(_cube.x, _cube.z);

        public static PHexCube ToCube(this PHexAxial _axial) =>
            new PHexCube(_axial.col, -_axial.col - _axial.row, _axial.row);

        #endregion

        public static Coord SetCol(this Coord _pPoint, float _col)
        {
            var axialPixel = new Coord(m_Shaper.m_PixelToAxial.Multiply(_pPoint)).SetY(_col);
            return new Coord(m_Shaper.m_AxialToPixel.Multiply(axialPixel));
        }

        public static Coord SetRow(this Coord _pPoint, float _row)
        {
            var axialPixel = new Coord(m_Shaper.m_PixelToAxial.Multiply(_pPoint)).SetX(_row);
            return new Coord(m_Shaper.m_AxialToPixel.Multiply(axialPixel));
        }

        public static bool InRange(this PHexCube _cube, int _radius) => Mathf.Abs(_cube.x) <= _radius &&
                                                                       Mathf.Abs(_cube.y) <= _radius &&
                                                                       Mathf.Abs(_cube.z) <= _radius;

        public static bool InRange(this PHexAxial _axial, int _radius) => _axial.ToCube().InRange(_radius);

        public static int Distance(this PHexAxial _axial1, PHexAxial _axial2) =>
            Distance((PHexCube) _axial1, (PHexCube) _axial2);

        public static int Distance(this PHexCube _cube1, PHexCube _cube2) => (Mathf.Abs(_cube1.x - _cube2.x) +
            Mathf.Abs(_cube1.y - _cube2.y) +
            Mathf.Abs(_cube1.z - _cube2.z)) / 2;

        public static PHexCube Rotate(this PHexCube _cube, int _60degClockWiseCount)
        {
            var x = _cube.x;
            var y = _cube.y;
            var z = _cube.z;
            switch (_60degClockWiseCount % 6)
            {
                default: return _cube;
                case 1: return new PHexCube(-z, -x, -y);
                case 2: return new PHexCube(y, z, x);
                case 3: return new PHexCube(-x, -y, -z);
                case 4: return new PHexCube(-y, -z, -x);
                case 5: return new PHexCube(z, x, y);
            }
        }

        public static PHexCube RotateAround(this PHexCube _cube, PHexCube _dst, int _60degClockWiseCount)
        {
            PHexCube offset = _dst - _cube;
            return _dst + offset.Rotate(_60degClockWiseCount);
        }

        public static PHexCube Reflect(this PHexCube _cube, ECubeAxis _axis)
        {
            var x = _cube.x;
            var y = _cube.y;
            var z = _cube.z;
            switch (_axis)
            {
                default: throw new Exception("Invalid Axis:" + _axis);
                case ECubeAxis.X: return new PHexCube(x, z, y);
                case ECubeAxis.Y: return new PHexCube(z, y, x);
                case ECubeAxis.Z: return new PHexCube(y, x, z);
            }
        }

        public static PHexCube ReflectAround(this PHexCube _cube, PHexCube _dst, ECubeAxis _axis)
        {
            PHexCube offset = _dst - _cube;
            return _dst + offset.Reflect(_axis);
        }

        public static PHexCube RotateMirror(int _radius, int _60degClockWiseCount)
        {
            return new PHexCube(2 * _radius + 1, -_radius, -_radius - 1).Rotate(_60degClockWiseCount);
        }

        public static IEnumerable<PHexCube> GetCoordsInRadius(this PHexCube _cube, int _radius)
        {
            foreach (var axial in _cube.ToAxial().GetCoordsInRadius(_radius))
                yield return axial.ToCube();
        }

        static readonly PHexAxial[] m_AxialNearbyCoords =
        {
            new PHexAxial(1, 0), new PHexAxial(1, -1), new PHexAxial(0, -1),
            new PHexAxial(-1, 0), new PHexAxial(-1, 1), new PHexAxial(0, 1)
        };

        public static IEnumerable<PHexAxial> GetCoordsNearby(this PHexAxial _axial)
        {
            foreach (PHexAxial nearbyCoords in m_AxialNearbyCoords)
                yield return _axial + nearbyCoords;
        }

        public static IEnumerable<PHexAxial> GetCoordsInRadius(this PHexAxial _axial, int _radius)
        {
            for (int i = -_radius; i <= _radius; i++)
            for (int j = -_radius; j <= _radius; j++)
            {
                var offset = new PHexAxial(i, j);
                if (!offset.InRange(_radius))
                    continue;
                yield return _axial + offset;
            }
        }

        
        //Range
        static readonly PHexCube[] m_CubeNearbyCoords =
        {
            new PHexCube(1, -1, 0), new PHexCube(1, 0, -1), new PHexCube(0, 1, -1),
            new PHexCube(-1, 1, 0), new PHexCube(-1, 0, 1), new PHexCube(0, -1, 1)
        };

        public static IEnumerable<PHexCube> GetCoordsNearby(PHexCube _cube)
        {
            foreach (PHexCube nearbyCoords in m_CubeNearbyCoords)
                yield return _cube + nearbyCoords;
        }

        public static PHexCube GetCoordsNearby(PHexCube _cube, int direction)
        {
            return _cube + m_AxialNearbyCoords[direction % 6];
        }
        public static IEnumerable<(int dir,bool first,PHexCube coord)> GetCoordsRinged(this PHexCube _center,int _radius)
        {
            if (_radius == 0)
                yield return (-1,true,_center);

            var ringIterate = _center + m_CubeNearbyCoords[4] * _radius;
            for(int i=0;i<6;i++)
            for (int j = 0; j < _radius; j++)
            {
                yield return (i,j==0,ringIterate);
                ringIterate += m_CubeNearbyCoords[i];
            }
        }

        public static int GetCoordsRingedCount(this PHexCube _center, int _radius) => _radius==0?1:_radius * 6;
    }
}