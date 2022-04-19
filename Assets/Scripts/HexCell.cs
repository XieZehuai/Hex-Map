using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个独立的六边形单元格，只保存数据，不负责与视觉（模型，网格等）相关的事情
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public Color color;

        public int Elevation
        {
            get => elevation;
            set
            {
                elevation = value;

                Vector3 position = transform.localPosition;
                position.y = elevation * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;
            }
        }

        public Vector3 Position => transform.localPosition;

        public RectTransform uiRect;

        private int elevation;

        [SerializeField] HexCell[] neighbors = default;

        /// <summary>
        /// 获取目标方向上的邻居，如果没有则返回 null
        /// </summary>
        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int)direction];
        }

        /// <summary>
        /// 设置单元格在目标方向上的邻居，同时也会将该邻居单元格在反方向上的邻居设置为当前单元格
        /// </summary>
        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int)direction] = cell;
            // 相邻关系是双向的，所以把目标单元格在反方向上的邻居设置为当前节点，
            // 注意不能调用cell.SetNeighbor，这样会陷入死循环
            cell.neighbors[(int)direction.Opposite()] = this;
        }

        public HexEdgeType GetEdgeType(HexDirection direction)
        {
            return HexMetrics.GetEdgeType(elevation, GetNeighbor(direction).elevation);
        }

        /// <summary>
        /// 获取当前单元格与目标单元格之间的连接类型，连接类型只取绝于两者海拔差的绝对值，
        /// 与谁高谁低无关
        /// </summary>
        public HexEdgeType GetEdgeType(HexCell otherCell)
        {
            return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
        }
    }
}
