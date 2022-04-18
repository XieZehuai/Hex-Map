using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 包含一系列与六边形有关的数值
    /// </summary>
    public static class HexMetrics
    {
        /// <summary>
        /// 把六边形看成由六个相同的等边三角形组成，outerRadius就是每个三角形的边长
        /// </summary>
        public const float outerRadius = 10f;

        /// <summary>
        /// 把六边形看成由六个相同的等边三角形组成，innerRadius就是每个三角形的高
        /// </summary>
        public const float innerRadius = outerRadius * 0.866025404f;

        /// <summary>
        /// 单元格内固定区域占总大小的比例
        /// </summary>
        public const float solidFactor = 0.75f;

        /// <summary>
        /// 单元格内与相邻单元格之间过渡区域的比例
        /// </summary>
        public const float blendFactor = 1f - solidFactor;

        /// <summary>
        /// 六边形的六个顶点相对于其中心的位置，从最上面的顶点开始，按顺时针方向排列
        /// </summary>
        private static readonly Vector3[] corners =
        {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            // 这里额外保存第一个顶点，这样在获取下一个角时下标就不用取模
            new Vector3(0f, 0f, outerRadius),
        };

        /// <summary>
        /// 获取指定方向上三角形第一个角的坐标
        /// </summary>
        public static Vector3 GetFirstCorner(HexDirection direction)
        {
            return corners[(int)direction];
        }

        /// <summary>
        /// 获取指定方向上三角形第二个角的坐标
        /// </summary>
        public static Vector3 GetSecondCorner(HexDirection direction)
        {
            return corners[(int)direction + 1];
        }

        /// <summary>
        /// 获取指定方向上三角形固定区域的第一个角的坐标
        /// </summary>
        public static Vector3 GetFirstSolidCorner(HexDirection direction)
        {
            return corners[(int)direction] * solidFactor;
        }

        /// <summary>
        /// 获取指定方向上三角形固定区域的第二个角的坐标
        /// </summary>
        public static Vector3 GetSecondSolidCorner(HexDirection direction)
        {
            return corners[(int)direction + 1] * solidFactor;
        }
    }
}