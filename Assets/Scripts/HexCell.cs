using System.Linq;
using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个独立的六边形单元格，只保存数据，不负责与视觉（模型，网格等）相关的事情
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public HexGridChunk chunk;
        public RectTransform uiRect;

        [SerializeField] private HexCell[] neighbors = default; // 将字段标记为 SerializeField 可实现热重载

        #region 单元格基础属性
        private int elevation = int.MinValue;
        private Color color;

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

                /*
                 * 根据海拔扰动强度，修改单元格和 UI 的实际高度
                 */
                Vector3 position = transform.localPosition;
                position.y = elevation * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;

                /*
                 * 因为河流无法向高处流，所以修改海拔后要判断河流是否合法
                 */
                ValidateRivers();

                /*
                 * 道路无法太陡峭，所以改变海拔后要检查道路的合法性
                 */
                for (int i = 0; i < roads.Length; i++)
                {
                    if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    {
                        SetRoad(i, false);
                    }
                }

                Refresh();
            }
        }

        public Vector3 Position => transform.localPosition;
        #endregion

        #region 道路相关属性
        /// <summary>
        /// 单元格上的道路，与河流不同，一个单元格上的六个方向都可以有道路，所以用一个数组
        /// 来保存每个方向上道路的情况
        /// </summary>
        [SerializeField] private bool[] roads = new bool[6];

        public bool HasRoads => roads.Any(road => road);
        #endregion

        #region 河流相关属性
        /*
         * 对于一个单元格来说，它的河流可以分为没有河流以及有河流穿过，对于穿过的河流，可以
         * 将其分为流入和流出两个部分，如果单元格是河流的源头，那就只有流出部分，如果单元格
         * 是河流末尾，那就只有流入部分
         */
        private bool hasIncomingRiver; // 是否有河流流入
        private bool hasOutgoingRiver; // 是否有河流流出
        private HexDirection incomingRiver; // 河流流入的方向
        private HexDirection outgoingRiver; // 河流流出的方向

        public bool HasIncomingRiver => hasIncomingRiver;
        public bool HasOutgoingRiver => hasOutgoingRiver;
        public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;
        public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

        public HexDirection IncomingRiver => incomingRiver;
        public HexDirection OutgoingRiver => outgoingRiver;
        public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

        /// <summary>
        /// 河床高度，忽视海拔扰动的影响，这样才能让不同单元格之间的河床处于相同高度
        /// </summary>
        public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

        /// <summary>
        /// 河流水面高度，忽视海拔扰动的影响
        /// </summary>
        public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        #endregion

        #region 水域相关属性
        private int waterLevel;

        /// <summary>
        /// 单元格所处水域的水平面高度（单位海拔）
        /// <para>
        /// 与河流水面高度不同，河流处于单元格内部，是单元格内部的一部分，而水域则覆盖在
        /// 整个单元格之上；区分水域是为了让不同区域的水平面可以不同；当水平面高度大于海
        /// 拔时，单元格被水覆盖
        /// </para>
        /// </summary>
        public int WaterLevel
        {
            get => waterLevel;
            set
            {
                if (waterLevel == value) return;

                waterLevel = value;
                ValidateRivers();
                Refresh();
            }
        }

        /// <summary>
        /// 单元格是否被水覆盖
        /// </summary>
        public bool IsUnderWater => waterLevel > elevation;

        public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        #endregion

        #region 墙壁相关属性
        private bool walled;

        /// <summary>
        /// 单元格是否被墙壁包围。墙壁处于单元格与单元格之间的连接部分，不处于单元格内，且包围整个单元格。
        /// </summary>
        public bool Walled
        {
            get => walled;
            set
            {
                if (walled != value)
                {
                    walled = value;
                    Refresh();
                }
            }
        }
        #endregion

        #region 与相邻单元格有关的方法
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

        /// <summary>
        /// 获取目标方向的连接类型
        /// </summary>
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
        /// 获取当前单元格和其目标方向上相邻单元格之间的海拔差
        /// </summary>
        public int GetElevationDifference(HexDirection direction)
        {
            return Mathf.Abs(elevation - GetNeighbor(direction).elevation);
        }
        #endregion

        #region 河流相关方法
        /// <summary>
        /// 单元格在目标方向上是否有河流流过
        /// </summary>
        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return hasIncomingRiver && incomingRiver == direction ||
                   hasOutgoingRiver && outgoingRiver == direction;
        }

        /// <summary>
        /// 判断当前单元格上的河流是否能流向目标单元格
        /// <para>
        /// 河流无法向上流，所以当前单元格的海拔大于等于目标单元格的海拔时，河流能流向目标单元格；
        /// 当当前单元格被水覆盖，且水平面高度等于目标单元格的海拔时，河流能流向目标单元格，因为
        /// 水平面的高度与河流水平面的高度相同。
        /// </para>
        /// </summary>
        public bool IsValidRiverDestination(HexCell neighbor)
        {
            return neighbor != null && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
        }

        /// <summary>
        /// 在目标方向上添加一条流出的河流，会检测河流的合法性，只有合法才会设置河流（会覆盖该方向上的道路）
        /// </summary>
        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            // 河流无法流向相邻单元格，直接返回
            if (!IsValidRiverDestination(neighbor))
            {
                return;
            }

            // 因为只能有一个流出的河流，所以需要把原有的流出河流移除
            // 如果当前方向上有流入的河流，也需要移除
            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            hasOutgoingRiver = true;
            outgoingRiver = direction;

            // 对应方向上的相邻单元格也需要添加一条流入的河流
            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();

            // 道路无法覆盖河流，但河流可以覆盖道路，所以设置河流后把当前方向上的道路移除
            SetRoad((int)direction, false);
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

        private void ValidateRivers()
        {
            if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
            {
                RemoveOutgoingRiver();
            }
            if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
            {
                RemoveIncomingRiver();
            }
        }
        #endregion

        #region 道路相关方法
        /// <summary>
        /// 目标方向上是否有道路
        /// </summary>
        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int)direction];
        }

        /// <summary>
        /// 在目标方向上添加一条道路（无法覆盖该方向上的河流）
        /// </summary>
        public void AddRoad(HexDirection direction)
        {
            // 在同一个方向上，无法同时存在道路以及河流，所以要判断当前方向上是否有河流；
            // 如果与目标方向上的单元格之间海拔差太大，也不能设置道路
            if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
                GetElevationDifference(direction) <= 1)
            {
                SetRoad((int)direction, true);
            }
        }

        /// <summary>
        /// 移除单元格上所有的道路以及对应相邻单元格上的道路
        /// </summary>
        public void RemoveRoads()
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    SetRoad(i, false);
                }
            }
        }

        /// <summary>
        /// 设置目标方向上的道路及其对应邻居的道路
        /// </summary>
        private void SetRoad(int index, bool state)
        {
            roads[index] = state;
            neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;

            neighbors[index].RefreshSelfOnly();
            RefreshSelfOnly();
        }
        #endregion

        #region 单元格细节相关属性
        private int urbanLevel;
        private int farmLevel;
        private int plantLevel;

        /// <summary>
        /// 单元格的城市化水平，控制单元格内的建筑数量，0为最低水平，也就是没有任何建筑
        /// </summary>
        public int UrbanLevel
        {
            get => urbanLevel;
            set
            {
                if (urbanLevel != value)
                {
                    urbanLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int FarmLevel
        {
            get => farmLevel;
            set
            {
                if (farmLevel != value)
                {
                    farmLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int PlantLevel
        {
            get => plantLevel;
            set
            {
                if (plantLevel != value)
                {
                    plantLevel = value;
                    RefreshSelfOnly();
                }
            }
        }
        #endregion

        /// <summary>
        /// 刷新单元格所在区块以及与当前单元格相邻的区块（不是与区块相邻，而是与单元格相邻）
        /// </summary>
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

        /// <summary>
        /// 只刷新单元格所在的区块
        /// </summary>
        private void RefreshSelfOnly()
        {
            chunk.Refresh();
        }
    }
}
