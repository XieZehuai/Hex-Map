using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个由一系列六边形组成的网格
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private Mesh hexMesh;
        private MeshCollider meshCollider;

        // vertices，colors，triangles数组只在构建网格时用得到，其他时候都不需要，所以将
        // 其定义成静态，在不同的 HexMesh 之间共享，节省内存，当使用 20 * 20 个 Chunk 时，
        // 这可以节省 100 MB 内存
        private static List<Vector3> vertices = new List<Vector3>();
        private static List<Color> colors = new List<Color>();
        private static List<int> triangles = new List<int>();

        private void Awake()
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
            hexMesh.name = "Hex Mesh";
        }

        /// <summary>
        /// 根据给定的单元格数据生成网格
        /// </summary>
        public void Triangulate(HexCell[] cells)
        {
            hexMesh.Clear();
            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            for (int i = 0; i < cells.Length; i++)
            {
                Triangulate(cells[i]);
            }

            hexMesh.vertices = vertices.ToArray();
            hexMesh.colors = colors.ToArray();
            hexMesh.triangles = triangles.ToArray();

            hexMesh.RecalculateNormals(); // 重新计算法线
            meshCollider.sharedMesh = hexMesh;
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

            TriangulateEdgeFan(center, e, cell.Color);

            // 生成当前单元格与相邻单元格之间的过渡部分
            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, cell, e);
            }
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
        {
            AddTriangle(center, edge.v1, edge.v2);
            AddTriangleColor(color);
            AddTriangle(center, edge.v2, edge.v3);
            AddTriangleColor(color);
            AddTriangle(center, edge.v3, edge.v4);
            AddTriangleColor(color);
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
            EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v4 + bridge);

            if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(e1, cell, e2, neighbor);
            }
            else
            {
                TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
            }

            // 生成当前单元格、相邻单元格、下一方向相邻单元格，之间的三角形连接部分，并且一个单元格
            // 有三个矩形连接，但只有两个三角形连接
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor != null)
                {
                    Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
                    v5.y = nextNeighbor.Position.y;

                    if (cell.Elevation <= neighbor.Elevation)
                    {
                        if (cell.Elevation <= nextNeighbor.Elevation)
                        {
                            TriangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
                        }
                        else
                        {
                            TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
                        }
                    }
                    else if (neighbor.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
                    }
                }
            }
        }

        private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
        {
            AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            AddQuadColor(c1, c2);
            AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            AddQuadColor(c1, c2);
            AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            AddQuadColor(c1, c2);
        }

        /// <summary>
        /// 生成相邻单元格之间梯田形状的斜坡
        /// </summary>
        private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
        {
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

            TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = c2;
                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

                TriangulateEdgeStrip(e1, c1, e2, c2);
            }

            TriangulateEdgeStrip(e2, c2, end, endCell.Color);
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
                AddTriangle(bottom, left, right);
                AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
            }
        }

        private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
            Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

            AddTriangle(begin, v3, v4);
            AddTriangleColor(beginCell.Color, c3, c4);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3, v2 = v4;
                Color c1 = c3, c2 = c4;

                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);

                AddQuad(v1, v2, v3, v4);
                AddQuadColor(c1, c2, c3, c4);
            }

            AddQuad(v3, v4, left, right);
            AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        }

        private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

            TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

            TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
        {
            Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

            AddTriangleUnperturbed(Perturb(begin), v2, boundary);
            AddTriangleColor(beginCell.Color, c2, boundaryColor);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                AddTriangleUnperturbed(v1, v2, boundary);
                AddTriangleColor(c1, c2, boundaryColor);
            }

            AddTriangleUnperturbed(v2, Perturb(left), boundary);
            AddTriangleColor(c2, leftCell.Color, boundaryColor);
        }

        /// <summary>
        /// 使用给定的三个顶点生成一个三角形，顶点顺序遵循左手定则
        /// </summary>
        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(Perturb(v1));
            vertices.Add(Perturb(v2));
            vertices.Add(Perturb(v3));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        /// <summary>
        /// 为当前三角形添加三个顶点的颜色，顶点顺序遵循左手定则
        /// </summary>
        private void AddTriangleColor(Color c1, Color c2, Color c3)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
        }

        /// <summary>
        /// 为当前三角形添加三个顶点的颜色
        /// </summary>
        private void AddTriangleColor(Color c1)
        {
            colors.Add(c1);
            colors.Add(c1);
            colors.Add(c1);
        }

        /// <summary>
        /// 使用给定的四个顶点生成一个四边形，四边形由两个三角形组成，顶点顺序分别为
        /// [v1, v3, v2] 和 [v2, v3, v4]，顶点顺序遵循左手定则
        /// </summary>
        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(Perturb(v1));
            vertices.Add(Perturb(v2));
            vertices.Add(Perturb(v3));
            vertices.Add(Perturb(v4));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        /// <summary>
        /// 为当前四边形添加顶点的颜色，四边形由两个三角形组成，颜色顺序分别为
        /// [c1, c3, c2] 和 [c2, c3, c4]，顶点顺序遵循左手定则
        /// </summary>
        private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
            colors.Add(c4);
        }

        /// <summary>
        /// 为当前四边形添加顶点的颜色，四边形由两个三角形组成，颜色顺序分别为
        /// [c1, c2, c1] 和 [c1, c2, c2]，顶点顺序遵循左手定则
        /// </summary>
        private void AddQuadColor(Color c1, Color c2)
        {
            colors.Add(c1);
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c2);
        }

        /// <summary>
        /// 使用噪声扰动顶点的位置，形成不规则的六边形
        /// </summary>
        private Vector3 Perturb(Vector3 position)
        {
            Vector4 sample = (HexMetrics.SampleNoise(position) * 2f - Vector4.one) * HexMetrics.cellPerturbStrength;
            position.x += sample.x;
            // position.y += sample.y;
            position.z += sample.z;
            return position;
        }
    }
}
