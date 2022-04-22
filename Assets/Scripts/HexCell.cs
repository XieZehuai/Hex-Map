using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个独立的六边形单元格，只保存数据，不负责与视觉（模型，网格等）相关的事情
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        [SerializeField] private HexCell[] neighbors = default;

        private int elevation = int.MinValue;
        private Color color;

        /*
         * 对于一个单元格来说，它的河流可以分为没有河流以及有河流穿过，对于穿过的河流，可以
         * 将其分为流入和流出两个部分，如果单元格是河流的源头，那就只有流出部分，如果单元格
         * 是河流末尾，那就只有流入部分
         */
        private bool hasIncomingRiver; // 是否有河流流入
        private bool hasOutgoingRiver; // 是否有河流流出
        private HexDirection incomingRiver; // 河流流入的方向
        private HexDirection outgoingRiver; // 河流流出的方向

        public HexCoordinates coordinates;
        public HexGridChunk chunk;
        public RectTransform uiRect;

        public Color Color
        {
            get => color;
            set
            {
                if (color == value) return;

                color = value;
                Refresh();
            }
        }

        public int Elevation
        {
            get => elevation;
            set
            {
                if (elevation == value) return;

                elevation = value;

                Vector3 position = transform.localPosition;
                position.y = elevation * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;

                if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
                {
                    RemoveOutgoingRiver();
                }
                if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
                {
                    RemoveIncomingRiver();
                }

                Refresh();
            }
        }

        public Vector3 Position => transform.localPosition;

        public bool HasIncomingRiver => hasIncomingRiver;
        public bool HasOutgoingRiver => hasOutgoingRiver;
        public HexDirection IncomingRiver => incomingRiver;
        public HexDirection OutgoingRiver => outgoingRiver;

        /// <summary>
        /// 当前单元格是否有河流
        /// </summary>
        public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;

        /// <summary>
        /// 当前单元格是否是河流源头或末尾
        /// </summary>
        public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

        /// <summary>
        /// 河床高度，忽视海拔扰动的影响，这样才能让不同单元格之间的河床处于相同高度
        /// </summary>
        public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

        public float RiverSurfaceY => (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;

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

        /// <summary>
        /// 单元格在目标方向上是否有河流流过
        /// </summary>
        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return hasIncomingRiver && incomingRiver == direction ||
                   hasOutgoingRiver && outgoingRiver == direction;
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            // 河流流出的部分必须连接到相邻单元格上的流入部分，且河流无法向上流
            HexCell neighbor = GetNeighbor(direction);
            if (neighbor == null || neighbor.elevation > elevation)
            {
                return;
            }

            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            hasOutgoingRiver = true;
            outgoingRiver = direction;
            RefreshSelfOnly();

            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.RefreshSelfOnly();
        }

        /// <summary>
        /// 移除当前单元格上河流流出的部分
        /// </summary>
        public void RemoveOutgoingRiver()
        {
            if (!hasOutgoingRiver)
            {
                return;
            }

            hasOutgoingRiver = false;
            RefreshSelfOnly();

            // 移除了流出部分后，相邻单元格上与河流相接的流入部分也应该移除掉，且目前不支持河流流出
            // 地图边界，所以流出的河流必定连接着相邻单元格上流入的河流
            HexCell neighbor = GetNeighbor(outgoingRiver);
            neighbor.hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        /// <summary>
        /// 移除当前单元格上河流流入的部分
        /// </summary>
        public void RemoveIncomingRiver()
        {
            if (!hasIncomingRiver)
            {
                return;
            }

            hasIncomingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(incomingRiver);
            neighbor.hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        /// <summary>
        /// 移除当前单元格上的河流
        /// </summary>
        public void RemoveRiver()
        {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }

        private void Refresh()
        {
            if (chunk != null)
            {
                chunk.Refresh();

                for (int i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i] != null && neighbors[i].chunk != chunk)
                    {
                        neighbors[i].chunk.Refresh();
                    }
                }
            }
        }

        private void RefreshSelfOnly()
        {
            chunk.Refresh();
        }
    }
}
