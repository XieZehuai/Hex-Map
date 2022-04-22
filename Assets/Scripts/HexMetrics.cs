using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 包含一系列与六边形有关的数值
    /// </summary>
    public static class HexMetrics
    {
        #region 常量

        /// <summary>
        /// 每块六边形地图的大小，因为单个网格支持的顶点数量有限，所以需要把地图分块
        /// </summary>
        public const int chunkSizeX = 5, chunkSizeZ = 5;

        public const float outerToInner = 0.866025404f;
        public const float innerToOuter = 1f / outerToInner;

        /// <summary>
        /// 把六边形看成由六个相同的等边三角形组成，outerRadius就是每个三角形的边长
        /// </summary>
        public const float outerRadius = 10f;

        /// <summary>
        /// 把六边形看成由六个相同的等边三角形组成，innerRadius就是每个三角形的高
        /// </summary>
        public const float innerRadius = outerRadius * outerToInner;

        /// <summary>
        /// 单元格内固定区域占总大小的比例
        /// </summary>
        public const float solidFactor = 0.8f;

        /// <summary>
        /// 单元格内与相邻单元格之间过渡区域的比例
        /// </summary>
        public const float blendFactor = 1f - solidFactor;

        /// <summary>
        /// 每单位海拔的高度
        /// </summary>
        public const float elevationStep = 3f;

        /// <summary>
        /// 河流河床相对于其所在单元格的海拔高度
        /// </summary>
        public const float streamBedElevationOffset = -1f;

        /// <summary>
        /// 网格顶点位置被噪声扰动的强度
        /// </summary>
        public const float cellPerturbStrength = 0f;
        /// <summary>
        /// 单元格海拔扰动强度，让相同海拔的单元格的实际高度出现起伏
        /// </summary>
        public const float elevationPerturbStrength = 1.5f;

        /// <summary>
        /// 单元格之间梯田类型连接的台阶数量
        /// </summary>
        public const int terracesPerSlope = 2;
        public const int terraceSteps = terracesPerSlope * 2 + 1;
        public const float horizontalTerraceStepSize = 1f / terraceSteps;
        public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

        /// <summary>
        /// 噪声图，用该图产生噪声
        /// </summary>
        public static Texture2D noiseSource;
        /// <summary>
        /// 因为噪声贴图的大小有限，只能覆盖地图上非常小的一块区域，所以需要在采样时把 UV 缩小，
        /// 或者看成把贴图放大 1 / noiseScale 倍
        /// </summary>
        public const float noiseScale = 0.003f;

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

        #endregion

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

        public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        {
            return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
        }

        public static Vector3 GetBridge(HexDirection direction)
        {
            return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
        }

        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
        {
            float h = step * horizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;

            float v = ((step + 1) / 2) * verticalTerraceStepSize;
            a.y += (b.y - a.y) * v;

            return a;
        }

        public static Color TerraceLerp(Color a, Color b, int step)
        {
            float h = step * horizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            int delta = Mathf.Abs(elevation1 - elevation2);
            return delta == 0 ? HexEdgeType.Flat : delta == 1 ? HexEdgeType.Slope : HexEdgeType.Cliff;
        }

        /// <summary>
        /// 采样噪声图
        /// </summary>
        public static Vector4 SampleNoise(Vector3 position)
        {
            return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
        }
    }
}