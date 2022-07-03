using System;
using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 六角坐标，由 x，y，z 三个值组成，且满足 x + y + z = 0
    /// </summary>
    [Serializable]
    public struct HexCoordinates
    {
        [SerializeField] private int x;
        [SerializeField] private int z;

        public int X => x;
        public int Y => -x - z; // 在六角坐标中，x + y + z = 0，所以 y = 0 - x - z
        public int Z => z;

        public HexCoordinates(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public override string ToString()
        {
            return $"({X.ToString()}, {Y.ToString()}, {Z.ToString()})";
        }

        public string ToStringOnSeparateLines()
        {
            return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
        }

        /// <summary>
        /// 根据给定的二维坐标生成一个六角坐标
        /// </summary>
        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            return new HexCoordinates(x - z / 2, z);
        }

        /// <summary>
        /// 把世界空间下的坐标转换为以世界空间原点为坐标系原点的六角坐标
        /// </summary>
        public static HexCoordinates FromPosition(Vector3 position)
        {
            float x = position.x / (HexMetrics.innerRadius * 2f);
            float y = -x;

            float offset = position.z / (HexMetrics.outerRadius * 3f);
            x -= offset;
            y -= offset;

            int ix = Mathf.RoundToInt(x);
            int iy = Mathf.RoundToInt(y);
            int iz = Mathf.RoundToInt(-x - y);

            // 因为舍入的关系，在position接近单元格的边缘时，舍入后的坐标不在同一个
            // 单元格内，所以 ix + iy + iz != 0，这时候就通过把被舍入较多的那一轴重
            // 新计算，得到正确的坐标，这里只重新计算 x 和 z，因为创建六角坐标时只需
            // 要 x 和 z 轴的值，y 轴由计算得到
            if (ix + iy + iz != 0)
            {
                float dx = Mathf.Abs(x - ix);
                float dy = Mathf.Abs(y - iy);
                float dz = Mathf.Abs(-x - y - iz);

                if (dx > dy && dx > dz)
                {
                    ix = -iy - iz;
                }
                else if (dz > dy)
                {
                    iz = -ix - iy;
                }
            }

            return new HexCoordinates(ix, iz);
        }

        public int DistanceTo(HexCoordinates other)
        {
            return (Mathf.Abs(X - other.X) + Mathf.Abs(Y - other.Y) + Mathf.Abs(Z - other.Z)) / 2;
        }
    }
}