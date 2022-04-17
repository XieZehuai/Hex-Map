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
            Vector3 center = cell.transform.localPosition; // 单元格的中心

            // 生成三角形，第一个顶点为单元格中心，剩下两个顶点为两个角
            AddTriangle(center,
                        center + HexMetrics.GetFirstCorner(direction),
                        center + HexMetrics.GetSecondCorner(direction));

            // 设置三角形的颜色，中心为单元格的颜色，两个顶点的颜色与相邻的单元格融合
            HexCell previousNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
            HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
            HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;

            AddTriangleColor(cell.color,
                            (cell.color + previousNeighbor.color + neighbor.color) / 3f,
                            (cell.color + neighbor.color + nextNeighbor.color) / 3f);
        }

        /// <summary>
        /// 使用给定的三个顶点生成一个三角形，顶点顺序遵循右手定则
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
        /// 为当前三角形添加三个顶点的颜色
        /// </summary>
        private void AddTriangleColor(Color c1, Color c2, Color c3)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
        }
    }
}