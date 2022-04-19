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

        private List<Vector3> vertices;
        private List<Color> colors;
        private List<int> triangles;

        private void Awake()
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
            hexMesh.name = "Hex Mesh";

            vertices = new List<Vector3>();
            colors = new List<Color>();
            triangles = new List<int>();
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
            Vector3 center = cell.transform.localPosition; // 单元格的中心
            Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
            Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
            AddTriangle(center, v1, v2);
            AddTriangleColor(cell.color);

            // 生成当前单元格与相邻单元格之间的过渡部分
            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, cell, v1, v2);
            }
        }

        /// <summary>
        /// 生成单元格和其对应方向上的相邻单元格中间的矩形连接部分，以及和当前方向的下一个方向上的邻居
        /// 之间的三角形连接部分
        /// </summary>
        private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
        {
            HexCell neighbor = cell.GetNeighbor(direction);
            // 如果目标方向上没有相邻的单元格，也就不需要连接部分
            if (neighbor == null) return;

            // 生成单元格和其对应方向上的相邻单元格中间的矩形连接部分
            Vector3 bridge = HexMetrics.GetBridge(direction);
            Vector3 v3 = v1 + bridge;
            Vector3 v4 = v2 + bridge;
            v3.y = v4.y = neighbor.Elevation * HexMetrics.elevationStep;

            if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateTerraces(v1, v2, cell, v3, v4, neighbor);
            }
            else
            {
                AddQuad(v1, v2, v3, v4);
                AddQuadColor(cell.color, neighbor.color);
            }

            // 生成当前单元格、相邻单元格、下一方向相邻单元格，之间的三角形连接部分，并且一个单元格
            // 有三个矩形连接，但只有两个三角形连接
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor != null)
                {
                    Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
                    v5.y = nextNeighbor.Elevation * HexMetrics.elevationStep;

                    if (cell.Elevation <= neighbor.Elevation)
                    {
                        if (cell.Elevation <= nextNeighbor.Elevation)
                        {
                            TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                        }
                        else
                        {
                            TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                        }
                    }
                    else if (neighbor.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
                    }
                    else
                    {
                        TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// 生成相邻单元格之间梯田形状的斜坡
        /// </summary>
        private void TriangulateTerraces(Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
            Vector3 endLeft, Vector3 endRight, HexCell endCell)
        {
            Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

            AddQuad(beginLeft, beginRight, v3, v4);
            AddQuadColor(beginCell.color, c2);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3;
                Vector3 v2 = v4;
                Color c1 = c2;
                v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
                v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
                c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);

                AddQuad(v1, v2, v3, v4);
                AddQuadColor(c1, c2);
            }

            AddQuad(v3, v4, endLeft, endRight);
            AddQuadColor(c2, endCell.color);
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
                AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
            }
        }

        private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
            Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

            AddTriangle(begin, v3, v4);
            AddTriangleColor(beginCell.color, c3, c4);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3, v2 = v4;
                Color c1 = c3, c2 = c4;

                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
                c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);

                AddQuad(v1, v2, v3, v4);
                AddQuadColor(c1, c2, c3, c4);
            }

            AddQuad(v3, v4, left, right);
            AddQuadColor(c3, c4, leftCell.color, rightCell.color);
        }

        private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(begin, right, b);
            Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

            TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangle(left, right, boundary);
                AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
            }
        }

        private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
            Vector3 boundary = Vector3.Lerp(begin, left, b);
            Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

            TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangle(left, right, boundary);
                AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
            }
        }

        private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
        {
            Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

            AddTriangle(begin, v2, boundary);
            AddTriangleColor(beginCell.color, c2, boundaryColor);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, left, i);
                c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
                AddTriangle(v1, v2, boundary);
                AddTriangleColor(c1, c2, boundaryColor);
            }

            AddTriangle(v2, left, boundary);
            AddTriangleColor(c2, leftCell.color, boundaryColor);
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
            position.y += sample.y;
            position.z += sample.z;
            return position;
        }
    }
}