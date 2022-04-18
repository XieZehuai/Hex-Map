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
            // 过渡部分为 两条边上分割点 和 两条边上的角 组成的四边形

            // 生成三角形的固定部分
            Vector3 center = cell.transform.localPosition; // 单元格的中心
            Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
            Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
            Color c1 = cell.color;
            Color c2 = cell.color;

            AddTriangle(center, v1, v2);
            AddTriangleColor(cell.color, c1, c2);

            // 生成三角形的过渡部分
            Vector3 v3 = center + HexMetrics.GetFirstCorner(direction);
            Vector3 v4 = center + HexMetrics.GetSecondCorner(direction);

            // 获取当前方向上的三个邻居（一个正对当前方向，两个在当前方向的斜向上），
            // 让两个顶点的颜色与相邻单元格的颜色融合
            HexCell previousNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;
            Color c3 = (cell.color + previousNeighbor.color + neighbor.color) / 3f;
            Color c4 = (cell.color + neighbor.color + nextNeighbor.color) / 3f;

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        /// <summary>
        /// 使用给定的三个顶点生成一个三角形，顶点顺序遵循左手定则
        /// </summary>
        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
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
        /// 使用给定的四个顶点生成一个四边形，四边形由两个三角形组成，顶点顺序分别为
        /// [v1, v3, v2] 和 [v2, v3, v4]，顶点顺序遵循左手定则
        /// </summary>
        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        /// <summary>
        /// 为当前四边形添加顶点的颜色，四边形由两个三角形组成，顶点顺序分别为
        /// [c1, c3, c2] 和 [c2, c3, c4]，顶点顺序遵循左手定则
        /// </summary>
        private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
            colors.Add(c4);
        }
    }
}