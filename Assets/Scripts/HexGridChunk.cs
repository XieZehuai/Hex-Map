using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个六边形网格地图区块
    /// <para>
    /// 为了构造更大的地图，需要把地图给分成多个单独的区块
    /// </para>
    /// </summary>
    public class HexGridChunk : MonoBehaviour
    {
        [SerializeField] private HexMesh terrain = default;
        [SerializeField] private HexMesh river = default;
        [SerializeField] private HexMesh road = default;
        [SerializeField] private HexMesh water = default;
        [SerializeField] private HexMesh waterShore = default;

        private HexCell[] cells;
        private Canvas gridCanvas;
        private bool showRefresh = true; // 是否需要刷新当前区块

        private void Awake()
        {
            gridCanvas = GetComponentInChildren<Canvas>();

            cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
            ShowUI(false);
        }

        private void LateUpdate()
        {
            // 修改操作都是在 Update 中进行的，所以把区块的刷新放在 LateUpdate 里，这样就能统一刷新
            if (showRefresh)
            {
                Triangulate();
                showRefresh = false;
            }
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.chunk = this;
            cell.transform.SetParent(transform, false);
            cell.uiRect.SetParent(gridCanvas.transform, false);
        }

        public void ShowUI(bool visible)
        {
            gridCanvas.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 刷新当前区块，只设置刷新状态，实际刷新操作延迟执行
        /// </summary>
        public void Refresh()
        {
            showRefresh = true;
        }

        /// <summary>
        /// 根据给定的单元格数据生成网格
        /// </summary>
        public void Triangulate()
        {
            terrain.Clear();
            river.Clear();
            road.Clear();
            water.Clear();
            waterShore.Clear();

            for (int i = 0; i < cells.Length; i++)
            {
                Triangulate(cells[i]);
            }

            terrain.Apply();
            river.Apply();
            road.Apply();
            water.Apply();
            waterShore.Apply();
        }

        /// <summary>
        /// 为单元格生成网格
        /// </summary>
        private void Triangulate(HexCell cell)
        {
            // 在单元格的六个方向上生成三角形
            for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                Triangulate(direction, cell);
            }
        }

        /// <summary>
        /// 在单元格的指定方向上生成一个三角形，把单元格看成由六个相同的等边三角形组成，
        /// 每个方向上对应一个三角形
        /// </summary>
        private void Triangulate(HexDirection direction, HexCell cell)
        {
            // 把三角形看成由固定部分和过渡部分组成，固定部分为 中心 和 两条边上分割点 组成的三角形，
            // 过渡部分再分成中间的矩形部分和两边的三角形部分，中间的矩形用于和直接邻居过渡，两边的
            // 三角形用于和直接邻居与间接邻居过渡

            // 生成三角形的固定部分
            Vector3 center = cell.Position; // 单元格的中心
            EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction),
                center + HexMetrics.GetSecondSolidCorner(direction));

            if (cell.HasRiver)
            {
                if (cell.HasRiverThroughEdge(direction))
                {
                    e.v3.y = cell.StreamBedY;
                    if (cell.HasRiverBeginOrEnd)
                    {
                        TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                    }
                    else
                    {
                        TriangulateWithRiver(direction, cell, center, e);
                    }
                }
                else
                {
                    TriangulateAdjacentToRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateWithoutRiver(direction, cell, center, e);
            }

            // 生成当前单元格与相邻单元格之间的过渡部分
            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, cell, e);
            }

            if (cell.IsUnderWater)
            {
                TriangulateWater(direction, cell, center);
            }
        }

        /// <summary>
        /// 生成带有河流且河流不是源头或末尾的单元格三角形部分
        /// </summary>
        private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
        {
            Vector3 centerL, centerR;
            // 如果在当前方向的反方向上也有河流，说明是直流
            if (cell.HasRiverThroughEdge(direction.Opposite()))
            {
                centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
                centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                centerL = center;
                centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
                centerR = center;
            }
            else if (cell.HasRiverThroughEdge(direction.Next2()))
            {
                centerL = center;
                centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
                centerR = center;
            }
            // 否则说明河流在中心点转弯
            else
            {
                centerL = centerR = center;
            }

            center = Vector3.Lerp(centerL, centerR, 0.5f);

            EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f),
                                              Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
            m.v3.y = center.y = e.v3.y;

            TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

            terrain.AddTriangle(centerL, m.v1, m.v2);
            terrain.AddTriangleColor(cell.Color);
            terrain.AddQuad(centerL, center, m.v2, m.v3);
            terrain.AddQuadColor(cell.Color);
            terrain.AddQuad(center, centerR, m.v3, m.v4);
            terrain.AddQuadColor(cell.Color);
            terrain.AddTriangle(centerR, m.v4, m.v5);
            terrain.AddTriangleColor(cell.Color);

            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }

        /// <summary>
        /// 生成带有河流且河流是源头或末尾的单元格三角形部分
        /// </summary>
        private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell,
            Vector3 center, EdgeVertices e)
        {
            EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
            m.v3.y = e.v3.y;

            TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
            TriangulateEdgeFan(center, m, cell.Color);

            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);

            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            river.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                river.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                river.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
        }

        /// <summary>
        /// 生成与河流相邻的单元格三角形部分
        /// </summary>
        private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell,
            Vector3 center, EdgeVertices e)
        {
            if (cell.HasRoads)
            {
                TriangulateRoadAdjacentRiver(direction, cell, center, e);
            }

            if (cell.HasRiverThroughEdge(direction.Next()))
            {
                if (cell.HasRiverThroughEdge(direction.Previous()))
                {
                    center += HexMetrics.GetSolidEdgeMiddle(direction) * (0.5f * HexMetrics.innerToOuter);
                }
                else if (cell.HasRiverThroughEdge(direction.Previous2()))
                {
                    center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
                }
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
            {
                center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
            }

            EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));

            TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
            TriangulateEdgeFan(center, m, cell.Color);
        }

        /// <summary>
        /// 生成不带有河流的单元格三角形部分（这里的不带有河流是指整个单元格都没有河流）
        /// </summary>
        private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
        {
            TriangulateEdgeFan(center, e, cell.Color);

            if (cell.HasRoads)
            {
                Vector2 interpolators = GetRoadInterpolators(direction, cell);

                TriangulateRoad(center, Vector3.Lerp(center, e.v1, interpolators.x),
                    Vector3.Lerp(center, e.v5, interpolators.y), e, cell.HasRoadThroughEdge(direction));
            }
        }

        private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
        {
            center.y = cell.WaterSurfaceY;

            HexCell neighbor = cell.GetNeighbor(direction);
            if (neighbor != null && !neighbor.IsUnderWater)
            {
                TriangulateWaterShore(direction, cell, neighbor, center);
            }
            else
            {
                TriangulateOpenWater(direction, cell, neighbor, center);
            }
        }

        private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
        {
            Vector3 c1 = center + HexMetrics.GetFirstSolidCorner(direction);
            Vector3 c2 = center + HexMetrics.GetSecondSolidCorner(direction);
            water.AddTriangle(center, c1, c2);

            if (direction <= HexDirection.SE && neighbor != null)
            {
                Vector3 bridge = HexMetrics.GetBridge(direction);
                Vector3 e1 = c1 + bridge;
                Vector3 e2 = c2 + bridge;

                water.AddQuad(c1, c2, e1, e2);

                if (direction <= HexDirection.E)
                {
                    HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                    if (nextNeighbor == null || !nextNeighbor.IsUnderWater) return;

                    water.AddTriangle(c2, e2, c2 + HexMetrics.GetBridge(direction.Next()));
                }
            }
        }

        private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
        {
            EdgeVertices e1 = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction),
                                               center + HexMetrics.GetSecondSolidCorner(direction));

            water.AddTriangle(center, e1.v1, e1.v2);
            water.AddTriangle(center, e1.v2, e1.v3);
            water.AddTriangle(center, e1.v3, e1.v4);
            water.AddTriangle(center, e1.v4, e1.v5);

            Vector3 bridge = HexMetrics.GetBridge(direction);
            EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);

            HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor != null)
            {
                waterShore.AddTriangle(e1.v5, e2.v5, e1.v5 + HexMetrics.GetBridge(direction.Next()));
                
                waterShore.AddTriangleUV(new Vector2(0f, 0f),
                                         new Vector2(0f, 1f),
                                         new Vector2(0f, nextNeighbor.IsUnderWater ? 0f : 1f));
            }
        }

        /// <summary>
        /// 生成单元格上的三角形
        /// <para>
        /// 将单元格看成由六个三角形组成，这一步是生成其中的一个三角形
        /// </para>
        /// </summary>
        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
        {
            terrain.AddTriangle(center, edge.v1, edge.v2);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v2, edge.v3);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v3, edge.v4);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v4, edge.v5);
            terrain.AddTriangleColor(color);
        }

        /// <summary>
        /// 生成单元格和其对应方向上的相邻单元格中间的矩形连接部分，以及和当前方向的下一个方向上的邻居
        /// 之间的三角形连接部分
        /// </summary>
        private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
        {
            HexCell neighbor = cell.GetNeighbor(direction);
            // 如果目标方向上没有相邻的单元格，也就不需要连接部分
            if (neighbor == null) return;

            // 生成单元格和其对应方向上的相邻单元格中间的矩形连接部分
            Vector3 bridge = HexMetrics.GetBridge(direction);
            bridge.y = neighbor.Position.y - cell.Position.y;
            EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

            if (neighbor.HasRiverThroughEdge(direction.Opposite()))
            {
                e2.v3.y = neighbor.StreamBedY;

                TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
                    cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                    cell.HasIncomingRiver && cell.IncomingRiver == direction);
            }

            // 判断单元格与相邻单元格之间的连接类型
            if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(e1, cell, e2, neighbor, cell.HasRoadThroughEdge(direction));
            }
            else
            {
                TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color, cell.HasRoadThroughEdge(direction));
            }

            // 生成当前单元格、相邻单元格、下一方向相邻单元格，之间的三角形连接部分，并且一个单元格
            // 有三个矩形连接，但只有两个三角形连接
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor != null)
                {
                    Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
                    v5.y = nextNeighbor.Position.y;

                    if (cell.Elevation <= neighbor.Elevation)
                    {
                        if (cell.Elevation <= nextNeighbor.Elevation)
                        {
                            TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                        }
                        else
                        {
                            TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                        }
                    }
                    else if (neighbor.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                    }
                }
            }
        }

        private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false)
        {
            terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            terrain.AddQuadColor(c1, c2);

            if (hasRoad)
            {
                TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
            }
        }

        /// <summary>
        /// 生成相邻单元格之间梯田形状的斜坡
        /// </summary>
        private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell,
            EdgeVertices end, HexCell endCell, bool hasRoad)
        {
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

            TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = c2;
                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

                TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
            }

            TriangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
        }

        private void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoad)
        {
            if (hasRoad)
            {
                Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
                TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);

                road.AddTriangle(center, mL, mC);
                road.AddTriangle(center, mC, mR);
                road.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
                road.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            }
            else
            {
                TriangulateRoadEdge(center, mL, mR);
            }
        }

        private void TriangulateRoadAdjacentRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
        {
            bool hasRoad = cell.HasRoadThroughEdge(direction);
            bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
            bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());

            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            Vector3 roadCenter = center;

            if (cell.HasRiverBeginOrEnd)
            {
                roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
            {
                Vector3 corner;
                if (previousHasRiver)
                {
                    if (!hasRoad && !cell.HasRoadThroughEdge(direction.Next())) return;

                    corner = HexMetrics.GetSecondSolidCorner(direction);
                }
                else
                {
                    if (!hasRoad && !cell.HasRoadThroughEdge(direction.Previous())) return;

                    corner = HexMetrics.GetFirstSolidCorner(direction);
                }

                roadCenter += corner * 0.5f;
                center += corner * 0.25f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
            {
                roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
            {
                roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
            }
            else if (previousHasRiver && nextHasRiver)
            {
                if (!hasRoad) return;

                Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
                roadCenter += offset * 0.75f;
                center += offset * 0.5f;
            }
            else
            {
                HexDirection middle;
                if (previousHasRiver)
                {
                    middle = direction.Next();
                }
                else if (nextHasRiver)
                {
                    middle = direction.Previous();
                }
                else
                {
                    middle = direction;
                }

                if (!cell.HasRoadThroughEdge(middle) &&
                    !cell.HasRoadThroughEdge(middle.Previous()) &&
                    !cell.HasRoadThroughEdge(middle.Next())) return;

                roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
            }

            Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
            Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
            TriangulateRoad(roadCenter, mL, mR, e, hasRoad);

            if (previousHasRiver)
            {
                TriangulateRoadEdge(roadCenter, center, mL);
            }
            if (nextHasRiver)
            {
                TriangulateRoadEdge(roadCenter, mR, center);
            }
        }

        /// <summary>
        /// 生成道路片段，道路直接覆盖在单元格之上，所以直接生成面片就行
        /// </summary>
        private void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3,
            Vector3 v4, Vector3 v5, Vector3 v6)
        {
            road.AddQuad(v1, v2, v4, v5);
            road.AddQuad(v2, v3, v5, v6);
            road.AddQuadUV(0f, 1f, 0f, 0f);
            road.AddQuadUV(1f, 0f, 0f, 0f);
        }

        private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
        {
            road.AddTriangle(center, mL, mR);
            road.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        }

        private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
        {
            Vector2 interpolators;

            if (cell.HasRoadThroughEdge(direction))
            {
                interpolators.x = interpolators.y = 0.5f;
            }
            else
            {
                interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
                interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
            }

            return interpolators;
        }

        /// <summary>
        /// 生成相邻的三个单元格之间的三角形连接部分，因为这种情况比较复杂，所以输入参数需要
        /// 遵循一定规则，bottom 表示坡度最低的单元格，left 表示剩下两个单元格中相对于坡度最
        /// 低的单元格位置偏左的单元格，right 就是偏右的单元格
        /// </summary>
        private void TriangulateCorner(Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
            HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

            if (leftEdgeType == HexEdgeType.Slope)
            {
                if (rightEdgeType == HexEdgeType.Slope)
                {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                }
                else
                {
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (rightEdgeType == HexEdgeType.Slope)
            {
                if (leftEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                {
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                if (leftCell.Elevation < rightCell.Elevation)
                {
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                {
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                }
            }
            else
            {
                terrain.AddTriangle(bottom, left, right);
                terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
            }
        }

        private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
            Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

            terrain.AddTriangle(begin, v3, v4);
            terrain.AddTriangleColor(beginCell.Color, c3, c4);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3, v2 = v4;
                Color c1 = c3, c2 = c4;

                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);

                terrain.AddQuad(v1, v2, v3, v4);
                terrain.AddQuadColor(c1, c2, c3, c4);
            }

            terrain.AddQuad(v3, v4, left, right);
            terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        }

        private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

            TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

            TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
        {
            Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
            terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                terrain.AddTriangleUnperturbed(v1, v2, boundary);
                terrain.AddTriangleColor(c1, c2, boundaryColor);
            }

            terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
            terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
        }
        private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y, float v, bool reversed)
        {
            TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
        }

        private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y1, float y2, float v, bool reversed)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            river.AddQuad(v1, v2, v3, v4);

            if (reversed)
            {
                river.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
            }
            else
            {
                river.AddQuadUV(0f, 1f, v, v + 0.2f);
            }
        }
    }
}